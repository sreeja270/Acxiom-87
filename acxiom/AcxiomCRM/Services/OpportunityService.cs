using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.ViewModels.Common;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services
{
    public interface IOpportunityService
    {
        Task<PaginatedList<Opportunity>> GetOpportunitiesAsync(UserContext userContext, string? searchTerm, string? stageFilter, string? statusFilter, string? assignedToFilter, string? sortOrder, int pageIndex, int pageSize);
        Task<Opportunity?> GetOpportunityByIdAsync(int id, UserContext userContext);
        Task<(bool Success, string Message, Opportunity? Opportunity)> CreateOpportunityAsync(Opportunity opportunity, UserContext userContext);
        Task<(bool Success, string Message, Opportunity? Opportunity)> UpdateOpportunityAsync(Opportunity opportunity, UserContext userContext);
        Task<(bool Success, string Message)> DeleteOrArchiveOpportunityAsync(int id, UserContext userContext);
        Task<(decimal TotalPipeline, decimal WeightedPipeline)> GetPipelineTotalsAsync(UserContext userContext);
    }

    public class OpportunityService : IOpportunityService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly ILogger<OpportunityService> _logger;

        public OpportunityService(ApplicationDbContext context, IAuditService auditService, ILogger<OpportunityService> logger)
        {
            _context = context;
            _auditService = auditService;
            _logger = logger;
        }

        private IQueryable<Opportunity> ApplyUserScope(IQueryable<Opportunity> query, UserContext userContext)
        {
            if (userContext.IsSalesExecutive)
            {
                return query.Where(o => o.AssignedToId == userContext.UserId);
            }
            return query;
        }

        public async Task<PaginatedList<Opportunity>> GetOpportunitiesAsync(
            UserContext userContext,
            string? searchTerm,
            string? stageFilter,
            string? statusFilter,
            string? assignedToFilter,
            string? sortOrder,
            int pageIndex,
            int pageSize)
        {
            var query = _context.Opportunities
                .Include(o => o.Customer)
                .Include(o => o.Lead)
                .Include(o => o.AssignedTo)
                .AsNoTracking()
                .AsQueryable();

            query = ApplyUserScope(query, userContext);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(o =>
                    o.OpportunityName.ToLower().Contains(term) ||
                    o.OpportunityCode.ToLower().Contains(term) ||
                    o.Customer!.CustomerName.ToLower().Contains(term) ||
                    (o.Customer.CompanyName != null && o.Customer.CompanyName.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(stageFilter))
            {
                query = query.Where(o => o.Stage == stageFilter);
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(o => o.Status == statusFilter);
            }

            if (!string.IsNullOrWhiteSpace(assignedToFilter) && !userContext.IsSalesExecutive)
            {
                query = query.Where(o => o.AssignedToId == assignedToFilter);
            }

            query = sortOrder switch
            {
                "name_desc" => query.OrderByDescending(o => o.OpportunityName),
                "amount_desc" => query.OrderByDescending(o => (double)o.Amount),
                "amount_asc" => query.OrderBy(o => (double)o.Amount),
                "date_asc" => query.OrderBy(o => o.ExpectedCloseDate),
                "date_desc" => query.OrderByDescending(o => o.ExpectedCloseDate),
                "stage" => query.OrderBy(o => o.Stage),
                _ => query.OrderByDescending(o => o.CreatedDate)
            };

            return await PaginatedList<Opportunity>.CreateAsync(query, pageIndex, pageSize);
        }

        public async Task<Opportunity?> GetOpportunityByIdAsync(int id, UserContext userContext)
        {
            var query = _context.Opportunities
                .Include(o => o.Customer)
                .Include(o => o.Lead)
                .Include(o => o.AssignedTo)
                .AsQueryable();

            var opportunity = await query.FirstOrDefaultAsync(o => o.OpportunityId == id);
            if (opportunity == null)
            {
                return null;
            }

            if (userContext.IsSalesExecutive && opportunity.AssignedToId != userContext.UserId)
            {
                throw new UnauthorizedAccessException("You are not authorized to view this opportunity.");
            }

            return opportunity;
        }

        public async Task<(bool Success, string Message, Opportunity? Opportunity)> CreateOpportunityAsync(Opportunity opportunity, UserContext userContext)
        {
            // Business Rule: Amount cannot be negative and must be > 0 for active
            if (opportunity.Amount <= 0)
            {
                return (false, "Opportunity Amount must be greater than 0.", null);
            }

            // Business Rule: Probability 0 to 100
            if (opportunity.Probability < 0 || opportunity.Probability > 100)
            {
                return (false, "Probability must be between 0 and 100.", null);
            }

            // Business Rule: Expected close date cannot be in the past for active
            if (opportunity.Status == "Open" && opportunity.ExpectedCloseDate.Date < DateTime.Today)
            {
                return (false, "Expected Close Date cannot be in the past.", null);
            }

            if (string.IsNullOrWhiteSpace(opportunity.OpportunityName))
            {
                return (false, "Opportunity Name is required.", null);
            }

            if (string.IsNullOrWhiteSpace(opportunity.OpportunityCode))
            {
                var nextNum = (await _context.Opportunities.CountAsync()) + 3001;
                opportunity.OpportunityCode = $"OPP-{nextNum}";
            }

            if (userContext.IsSalesExecutive)
            {
                opportunity.AssignedToId = userContext.UserId;
            }

            // Align status with stage
            if (opportunity.Stage == "Won")
            {
                opportunity.Status = "Won";
                opportunity.Probability = 100;
            }
            else if (opportunity.Stage == "Lost")
            {
                opportunity.Status = "Lost";
                opportunity.Probability = 0;
            }
            else
            {
                opportunity.Status = "Open";
            }

            opportunity.CreatedDate = DateTime.UtcNow;
            opportunity.CreatedBy = userContext.UserEmail;

            _context.Opportunities.Add(opportunity);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userContext.UserId,
                userContext.UserEmail,
                "Create",
                "Opportunity",
                opportunity.OpportunityId.ToString(),
                null,
                $"Code: {opportunity.OpportunityCode}, Name: {opportunity.OpportunityName}, Amount: {opportunity.Amount:C}, Stage: {opportunity.Stage}",
                $"Created opportunity {opportunity.OpportunityName} for customer {opportunity.CustomerId}",
                userContext.IpAddress);

            return (true, "Opportunity created successfully.", opportunity);
        }

        public async Task<(bool Success, string Message, Opportunity? Opportunity)> UpdateOpportunityAsync(Opportunity opportunity, UserContext userContext)
        {
            var existing = await _context.Opportunities.FirstOrDefaultAsync(o => o.OpportunityId == opportunity.OpportunityId);
            if (existing == null)
            {
                return (false, "Opportunity not found.", null);
            }

            if (userContext.IsSalesExecutive && existing.AssignedToId != userContext.UserId)
            {
                throw new UnauthorizedAccessException("You are not authorized to modify this opportunity.");
            }

            if (opportunity.Amount <= 0)
            {
                return (false, "Opportunity Amount must be greater than 0.", null);
            }

            if (opportunity.Probability < 0 || opportunity.Probability > 100)
            {
                return (false, "Probability must be between 0 and 100.", null);
            }

            if (opportunity.Status == "Open" && opportunity.ExpectedCloseDate.Date < DateTime.Today)
            {
                return (false, "Expected Close Date cannot be in the past.", null);
            }

            var stageChanged = existing.Stage != opportunity.Stage;
            var oldSummary = $"Stage: {existing.Stage}, Amount: {existing.Amount:C}, Prob: {existing.Probability}%, CloseDate: {existing.ExpectedCloseDate:yyyy-MM-dd}";

            existing.OpportunityName = opportunity.OpportunityName;
            existing.CustomerId = opportunity.CustomerId;
            existing.LeadId = opportunity.LeadId;
            existing.Amount = opportunity.Amount;
            existing.Stage = opportunity.Stage;
            existing.Probability = opportunity.Probability;
            existing.ExpectedCloseDate = opportunity.ExpectedCloseDate;
            existing.Description = opportunity.Description;

            if (opportunity.Stage == "Won")
            {
                existing.Status = "Won";
                existing.Probability = 100;
            }
            else if (opportunity.Stage == "Lost")
            {
                existing.Status = "Lost";
                existing.Probability = 0;
            }
            else
            {
                existing.Status = opportunity.Status;
            }

            if (!userContext.IsSalesExecutive)
            {
                existing.AssignedToId = opportunity.AssignedToId;
            }

            await _context.SaveChangesAsync();

            var newSummary = $"Stage: {existing.Stage}, Amount: {existing.Amount:C}, Prob: {existing.Probability}%, CloseDate: {existing.ExpectedCloseDate:yyyy-MM-dd}";

            await _auditService.LogAsync(
                userContext.UserId,
                userContext.UserEmail,
                stageChanged ? "Status Change" : "Update",
                "Opportunity",
                existing.OpportunityId.ToString(),
                oldSummary,
                newSummary,
                stageChanged ? $"Progressed opportunity to {existing.Stage}" : $"Updated opportunity {existing.OpportunityName}",
                userContext.IpAddress);

            return (true, "Opportunity updated successfully.", existing);
        }

        public async Task<(bool Success, string Message)> DeleteOrArchiveOpportunityAsync(int id, UserContext userContext)
        {
            var opportunity = await _context.Opportunities.FirstOrDefaultAsync(o => o.OpportunityId == id);
            if (opportunity == null)
            {
                return (false, "Opportunity not found.");
            }

            if (userContext.IsSalesExecutive && opportunity.AssignedToId != userContext.UserId)
            {
                throw new UnauthorizedAccessException("You are not authorized to modify this opportunity.");
            }

            _context.Opportunities.Remove(opportunity);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userContext.UserId,
                userContext.UserEmail,
                "Delete",
                "Opportunity",
                id.ToString(),
                $"Name: {opportunity.OpportunityName}, Code: {opportunity.OpportunityCode}",
                null,
                $"Deleted opportunity {opportunity.OpportunityName}",
                userContext.IpAddress);

            return (true, "Opportunity deleted successfully.");
        }

        public async Task<(decimal TotalPipeline, decimal WeightedPipeline)> GetPipelineTotalsAsync(UserContext userContext)
        {
            var query = _context.Opportunities.Where(o => o.Status == "Open").AsNoTracking();
            query = ApplyUserScope(query, userContext);

            var list = await query.Select(o => new { o.Amount, o.Probability }).ToListAsync();
            var total = list.Sum(o => o.Amount);
            var weighted = list.Sum(o => o.Amount * (decimal)o.Probability / 100m);

            return (total, Math.Round(weighted, 2));
        }
    }
}
