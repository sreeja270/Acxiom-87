using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.ViewModels.Common;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services
{
    public interface IActivityService
    {
        Task<PaginatedList<Activity>> GetActivitiesAsync(UserContext userContext, string? searchTerm, string? typeFilter, string? statusFilter, string? assignedToFilter, int pageIndex, int pageSize);
        Task<Activity?> GetActivityByIdAsync(int id, UserContext userContext);
        Task<(bool Success, string Message, Activity? Activity)> CreateActivityAsync(Activity activity, UserContext userContext);
        Task<(bool Success, string Message, Activity? Activity)> UpdateActivityAsync(Activity activity, UserContext userContext);
        Task<(bool Success, string Message)> DeleteActivityAsync(int id, UserContext userContext);
        Task<List<Activity>> GetTimelineForEntityAsync(int? customerId, int? leadId, UserContext userContext);
    }

    public class ActivityService : IActivityService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly ILogger<ActivityService> _logger;

        public ActivityService(ApplicationDbContext context, IAuditService auditService, ILogger<ActivityService> logger)
        {
            _context = context;
            _auditService = auditService;
            _logger = logger;
        }

        private IQueryable<Activity> ApplyUserScope(IQueryable<Activity> query, UserContext userContext)
        {
            if (userContext.IsSalesExecutive)
            {
                return query.Where(a => a.AssignedToId == userContext.UserId);
            }
            return query;
        }

        public async Task<PaginatedList<Activity>> GetActivitiesAsync(
            UserContext userContext,
            string? searchTerm,
            string? typeFilter,
            string? statusFilter,
            string? assignedToFilter,
            int pageIndex,
            int pageSize)
        {
            var query = _context.Activities
                .Include(a => a.Customer)
                .Include(a => a.Lead)
                .Include(a => a.AssignedTo)
                .AsNoTracking()
                .AsQueryable();

            query = ApplyUserScope(query, userContext);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(a =>
                    a.Subject.ToLower().Contains(term) ||
                    a.Description.ToLower().Contains(term) ||
                    (a.Customer != null && a.Customer.CustomerName.ToLower().Contains(term)) ||
                    (a.Lead != null && a.Lead.LeadName.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(typeFilter))
            {
                query = query.Where(a => a.ActivityType == typeFilter);
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(a => a.Status == statusFilter);
            }

            if (!string.IsNullOrWhiteSpace(assignedToFilter) && !userContext.IsSalesExecutive)
            {
                query = query.Where(a => a.AssignedToId == assignedToFilter);
            }

            query = query.OrderByDescending(a => a.ActivityDate);

            return await PaginatedList<Activity>.CreateAsync(query, pageIndex, pageSize);
        }

        public async Task<Activity?> GetActivityByIdAsync(int id, UserContext userContext)
        {
            var query = _context.Activities
                .Include(a => a.Customer)
                .Include(a => a.Lead)
                .Include(a => a.AssignedTo)
                .AsQueryable();

            var activity = await query.FirstOrDefaultAsync(a => a.ActivityId == id);
            if (activity == null)
            {
                return null;
            }

            if (userContext.IsSalesExecutive && activity.AssignedToId != userContext.UserId)
            {
                throw new UnauthorizedAccessException("You are not authorized to view this activity.");
            }

            return activity;
        }

        public async Task<(bool Success, string Message, Activity? Activity)> CreateActivityAsync(Activity activity, UserContext userContext)
        {
            if (string.IsNullOrWhiteSpace(activity.Subject))
            {
                return (false, "Subject is required.", null);
            }

            if (string.IsNullOrWhiteSpace(activity.Description))
            {
                return (false, "Description is required.", null);
            }

            if (userContext.IsSalesExecutive)
            {
                activity.AssignedToId = userContext.UserId;
            }

            activity.CreatedDate = DateTime.UtcNow;
            activity.CreatedBy = userContext.UserEmail;

            _context.Activities.Add(activity);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userContext.UserId,
                userContext.UserEmail,
                "Create",
                "Activity",
                activity.ActivityId.ToString(),
                null,
                $"Type: {activity.ActivityType}, Subject: {activity.Subject}, Status: {activity.Status}",
                $"Logged activity '{activity.Subject}'",
                userContext.IpAddress);

            return (true, "Activity recorded successfully.", activity);
        }

        public async Task<(bool Success, string Message, Activity? Activity)> UpdateActivityAsync(Activity activity, UserContext userContext)
        {
            var existing = await _context.Activities.FirstOrDefaultAsync(a => a.ActivityId == activity.ActivityId);
            if (existing == null)
            {
                return (false, "Activity not found.", null);
            }

            if (userContext.IsSalesExecutive && existing.AssignedToId != userContext.UserId)
            {
                throw new UnauthorizedAccessException("You are not authorized to modify this activity.");
            }

            var oldSummary = $"Type: {existing.ActivityType}, Subject: {existing.Subject}, Status: {existing.Status}";

            existing.ActivityType = activity.ActivityType;
            existing.Subject = activity.Subject;
            existing.Description = activity.Description;
            existing.ActivityDate = activity.ActivityDate;
            existing.CustomerId = activity.CustomerId;
            existing.LeadId = activity.LeadId;
            existing.Status = activity.Status;

            if (!userContext.IsSalesExecutive)
            {
                existing.AssignedToId = activity.AssignedToId;
            }

            await _context.SaveChangesAsync();

            var newSummary = $"Type: {existing.ActivityType}, Subject: {existing.Subject}, Status: {existing.Status}";

            await _auditService.LogAsync(
                userContext.UserId,
                userContext.UserEmail,
                "Update",
                "Activity",
                existing.ActivityId.ToString(),
                oldSummary,
                newSummary,
                $"Updated activity '{existing.Subject}'",
                userContext.IpAddress);

            return (true, "Activity updated successfully.", existing);
        }

        public async Task<(bool Success, string Message)> DeleteActivityAsync(int id, UserContext userContext)
        {
            var activity = await _context.Activities.FirstOrDefaultAsync(a => a.ActivityId == id);
            if (activity == null)
            {
                return (false, "Activity not found.");
            }

            if (userContext.IsSalesExecutive && activity.AssignedToId != userContext.UserId)
            {
                throw new UnauthorizedAccessException("You are not authorized to delete this activity.");
            }

            _context.Activities.Remove(activity);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userContext.UserId,
                userContext.UserEmail,
                "Delete",
                "Activity",
                id.ToString(),
                $"Subject: {activity.Subject}",
                null,
                $"Deleted activity #{id}",
                userContext.IpAddress);

            return (true, "Activity deleted successfully.");
        }

        public async Task<List<Activity>> GetTimelineForEntityAsync(int? customerId, int? leadId, UserContext userContext)
        {
            var query = _context.Activities
                .Include(a => a.AssignedTo)
                .AsNoTracking()
                .AsQueryable();

            query = ApplyUserScope(query, userContext);

            if (customerId.HasValue && leadId.HasValue)
            {
                query = query.Where(a => a.CustomerId == customerId.Value || a.LeadId == leadId.Value);
            }
            else if (customerId.HasValue)
            {
                query = query.Where(a => a.CustomerId == customerId.Value);
            }
            else if (leadId.HasValue)
            {
                query = query.Where(a => a.LeadId == leadId.Value);
            }

            return await query.OrderByDescending(a => a.ActivityDate).Take(25).ToListAsync();
        }
    }
}
