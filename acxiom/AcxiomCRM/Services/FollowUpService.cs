using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.ViewModels.Common;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services
{
    public interface IFollowUpService
    {
        Task<PaginatedList<FollowUp>> GetFollowUpsAsync(UserContext userContext, string? searchTerm, string? statusFilter, string? typeFilter, string? dateFilter, string? assignedToFilter, int pageIndex, int pageSize);
        Task<FollowUp?> GetFollowUpByIdAsync(int id, UserContext userContext);
        Task<(bool Success, string Message, FollowUp? FollowUp)> CreateFollowUpAsync(FollowUp followUp, UserContext userContext);
        Task<(bool Success, string Message, FollowUp? FollowUp)> UpdateFollowUpAsync(FollowUp followUp, UserContext userContext);
        Task<(bool Success, string Message)> UpdateStatusAsync(int id, string newStatus, string? notes, DateTime? newDate, UserContext userContext);
        Task<(bool Success, string Message)> DeleteFollowUpAsync(int id, UserContext userContext);
        Task<(int Overdue, int Today, int Upcoming)> GetFollowUpCountersAsync(UserContext userContext);
    }

    public class FollowUpService : IFollowUpService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly ILogger<FollowUpService> _logger;

        public FollowUpService(ApplicationDbContext context, IAuditService auditService, ILogger<FollowUpService> logger)
        {
            _context = context;
            _auditService = auditService;
            _logger = logger;
        }

        private IQueryable<FollowUp> ApplyUserScope(IQueryable<FollowUp> query, UserContext userContext)
        {
            if (userContext.IsSalesExecutive)
            {
                return query.Where(f => f.AssignedToId == userContext.UserId);
            }
            return query;
        }

        public async Task<PaginatedList<FollowUp>> GetFollowUpsAsync(
            UserContext userContext,
            string? searchTerm,
            string? statusFilter,
            string? typeFilter,
            string? dateFilter,
            string? assignedToFilter,
            int pageIndex,
            int pageSize)
        {
            var query = _context.FollowUps
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .Include(f => f.AssignedTo)
                .AsNoTracking()
                .AsQueryable();

            query = ApplyUserScope(query, userContext);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(f =>
                    f.Remarks.ToLower().Contains(term) ||
                    f.FollowUpType.ToLower().Contains(term) ||
                    (f.Customer != null && f.Customer.CustomerName.ToLower().Contains(term)) ||
                    (f.Lead != null && f.Lead.LeadName.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(f => f.Status == statusFilter);
            }

            if (!string.IsNullOrWhiteSpace(typeFilter))
            {
                query = query.Where(f => f.FollowUpType == typeFilter);
            }

            if (!string.IsNullOrWhiteSpace(dateFilter))
            {
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);
                if (dateFilter == "Today")
                {
                    query = query.Where(f => f.FollowUpDate >= today && f.FollowUpDate < tomorrow);
                }
                else if (dateFilter == "Upcoming")
                {
                    query = query.Where(f => f.FollowUpDate >= tomorrow && f.Status == "Planned");
                }
                else if (dateFilter == "Overdue")
                {
                    query = query.Where(f => f.FollowUpDate < today && f.Status == "Planned");
                }
            }

            if (!string.IsNullOrWhiteSpace(assignedToFilter) && !userContext.IsSalesExecutive)
            {
                query = query.Where(f => f.AssignedToId == assignedToFilter);
            }

            query = query.OrderBy(f => f.FollowUpDate);

            return await PaginatedList<FollowUp>.CreateAsync(query, pageIndex, pageSize);
        }

        public async Task<FollowUp?> GetFollowUpByIdAsync(int id, UserContext userContext)
        {
            var query = _context.FollowUps
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .Include(f => f.AssignedTo)
                .AsQueryable();

            var followUp = await query.FirstOrDefaultAsync(f => f.FollowUpId == id);
            if (followUp == null)
            {
                return null;
            }

            if (userContext.IsSalesExecutive && followUp.AssignedToId != userContext.UserId)
            {
                throw new UnauthorizedAccessException("You are not authorized to view this follow-up.");
            }

            return followUp;
        }

        public async Task<(bool Success, string Message, FollowUp? FollowUp)> CreateFollowUpAsync(FollowUp followUp, UserContext userContext)
        {
            // Business rule: New / Planned follow-up date cannot be earlier than today
            if (followUp.Status == "Planned" && followUp.FollowUpDate.Date < DateTime.Today)
            {
                return (false, "Follow-up date cannot be earlier than today.", null);
            }

            if (string.IsNullOrWhiteSpace(followUp.Remarks))
            {
                return (false, "Remarks are required.", null);
            }

            if (userContext.IsSalesExecutive)
            {
                followUp.AssignedToId = userContext.UserId;
            }

            followUp.CreatedDate = DateTime.UtcNow;
            followUp.CreatedBy = userContext.UserEmail;

            _context.FollowUps.Add(followUp);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userContext.UserId,
                userContext.UserEmail,
                "Create",
                "FollowUp",
                followUp.FollowUpId.ToString(),
                null,
                $"Type: {followUp.FollowUpType}, Date: {followUp.FollowUpDate:yyyy-MM-dd HH:mm}, Status: {followUp.Status}",
                $"Created follow-up for {followUp.FollowUpDate:yyyy-MM-dd}",
                userContext.IpAddress);

            return (true, "Follow-up scheduled successfully.", followUp);
        }

        public async Task<(bool Success, string Message, FollowUp? FollowUp)> UpdateFollowUpAsync(FollowUp followUp, UserContext userContext)
        {
            var existing = await _context.FollowUps.FirstOrDefaultAsync(f => f.FollowUpId == followUp.FollowUpId);
            if (existing == null)
            {
                return (false, "Follow-up not found.", null);
            }

            if (userContext.IsSalesExecutive && existing.AssignedToId != userContext.UserId)
            {
                throw new UnauthorizedAccessException("You are not authorized to modify this follow-up.");
            }

            var oldSummary = $"Type: {existing.FollowUpType}, Date: {existing.FollowUpDate:yyyy-MM-dd HH:mm}, Status: {existing.Status}";

            existing.CustomerId = followUp.CustomerId;
            existing.LeadId = followUp.LeadId;
            existing.FollowUpDate = followUp.FollowUpDate;
            existing.FollowUpType = followUp.FollowUpType;
            existing.Remarks = followUp.Remarks;
            existing.Status = followUp.Status;
            existing.CompletionNotes = followUp.CompletionNotes;

            if (followUp.Status == "Completed" && existing.CompletedDate == null)
            {
                existing.CompletedDate = DateTime.UtcNow;
            }

            if (!userContext.IsSalesExecutive)
            {
                existing.AssignedToId = followUp.AssignedToId;
            }

            await _context.SaveChangesAsync();

            var newSummary = $"Type: {existing.FollowUpType}, Date: {existing.FollowUpDate:yyyy-MM-dd HH:mm}, Status: {existing.Status}";

            await _auditService.LogAsync(
                userContext.UserId,
                userContext.UserEmail,
                "Update",
                "FollowUp",
                existing.FollowUpId.ToString(),
                oldSummary,
                newSummary,
                $"Updated follow-up #{existing.FollowUpId}",
                userContext.IpAddress);

            return (true, "Follow-up updated successfully.", existing);
        }

        public async Task<(bool Success, string Message)> UpdateStatusAsync(
            int id,
            string newStatus,
            string? notes,
            DateTime? newDate,
            UserContext userContext)
        {
            var followUp = await _context.FollowUps.FirstOrDefaultAsync(f => f.FollowUpId == id);
            if (followUp == null)
            {
                return (false, "Follow-up not found.");
            }

            if (userContext.IsSalesExecutive && followUp.AssignedToId != userContext.UserId)
            {
                throw new UnauthorizedAccessException("You are not authorized to update this follow-up.");
            }

            var oldSummary = $"Status: {followUp.Status}, Date: {followUp.FollowUpDate:yyyy-MM-dd HH:mm}";

            if (newStatus == "Completed")
            {
                followUp.Status = "Completed";
                followUp.CompletedDate = DateTime.UtcNow;
                if (!string.IsNullOrWhiteSpace(notes))
                {
                    followUp.CompletionNotes = notes;
                }
            }
            else if (newStatus == "Missed")
            {
                followUp.Status = "Missed";
            }
            else if (newStatus == "Cancelled")
            {
                followUp.Status = "Cancelled";
            }
            else if (newStatus == "Rescheduled" && newDate.HasValue)
            {
                if (newDate.Value.Date < DateTime.Today)
                {
                    return (false, "Follow-up date cannot be earlier than today.");
                }

                followUp.FollowUpDate = newDate.Value;
                followUp.Status = "Planned";
                if (!string.IsNullOrWhiteSpace(notes))
                {
                    followUp.Remarks += $" [Rescheduled: {notes}]";
                }
            }

            await _context.SaveChangesAsync();

            var newSummary = $"Status: {followUp.Status}, Date: {followUp.FollowUpDate:yyyy-MM-dd HH:mm}";

            await _auditService.LogAsync(
                userContext.UserId,
                userContext.UserEmail,
                "Status Change",
                "FollowUp",
                followUp.FollowUpId.ToString(),
                oldSummary,
                newSummary,
                $"Follow-up status transitioned to {followUp.Status}. {notes}",
                userContext.IpAddress);

            return (true, $"Follow-up marked as {followUp.Status}.");
        }

        public async Task<(bool Success, string Message)> DeleteFollowUpAsync(int id, UserContext userContext)
        {
            var followUp = await _context.FollowUps.FirstOrDefaultAsync(f => f.FollowUpId == id);
            if (followUp == null)
            {
                return (false, "Follow-up not found.");
            }

            if (userContext.IsSalesExecutive && followUp.AssignedToId != userContext.UserId)
            {
                throw new UnauthorizedAccessException("You are not authorized to delete this follow-up.");
            }

            _context.FollowUps.Remove(followUp);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userContext.UserId,
                userContext.UserEmail,
                "Delete",
                "FollowUp",
                id.ToString(),
                $"Type: {followUp.FollowUpType}, Date: {followUp.FollowUpDate}",
                null,
                $"Deleted follow-up #{id}",
                userContext.IpAddress);

            return (true, "Follow-up deleted successfully.");
        }

        public async Task<(int Overdue, int Today, int Upcoming)> GetFollowUpCountersAsync(UserContext userContext)
        {
            var query = _context.FollowUps.Where(f => f.Status == "Planned").AsNoTracking();
            query = ApplyUserScope(query, userContext);

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var overdue = await query.CountAsync(f => f.FollowUpDate < today);
            var forToday = await query.CountAsync(f => f.FollowUpDate >= today && f.FollowUpDate < tomorrow);
            var upcoming = await query.CountAsync(f => f.FollowUpDate >= tomorrow);

            return (overdue, forToday, upcoming);
        }
    }
}
