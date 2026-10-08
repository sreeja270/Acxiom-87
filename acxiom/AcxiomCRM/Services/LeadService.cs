using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.ViewModels;
using AcxiomCRM.ViewModels.Common;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services
{
    public interface ILeadService
    {
        Task<PaginatedList<Lead>> GetLeadsAsync(UserContext userContext, string? searchTerm, string? statusFilter, string? sourceFilter, string? priorityFilter, string? assignedToFilter, string? sortOrder, int pageIndex, int pageSize);
        Task<Lead?> GetLeadByIdAsync(int id, UserContext userContext);
        Task<(bool Success, string Message, Lead? Lead)> CreateLeadAsync(Lead lead, UserContext userContext);
        Task<(bool Success, string Message, Lead? Lead)> UpdateLeadAsync(Lead lead, UserContext userContext);
        Task<(bool Success, string Message)> DeleteOrDeactivateLeadAsync(int id, UserContext userContext);
        Task<(bool Success, string Message, Customer? Customer, Opportunity? Opportunity)> ConvertLeadAsync(int leadId, ConvertLeadViewModel model, UserContext userContext);
        Task<List<Lead>> GetOpenLeadsForDropdownAsync(UserContext userContext);
    }

    public class LeadService : ILeadService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly ICustomerService _customerService;
        private readonly ILogger<LeadService> _logger;

        public LeadService(
            ApplicationDbContext context,
            IAuditService auditService,
            ICustomerService customerService,
            ILogger<LeadService> logger)
        {
            _context = context;
            _auditService = auditService;
            _customerService = customerService;
            _logger = logger;
        }

        private IQueryable<Lead> ApplyUserScope(IQueryable<Lead> query, UserContext userContext)
        {
            if (userContext.IsSalesExecutive)
            {
                return query.Where(l => l.AssignedToId == userContext.UserId);
            }
            return query;
        }

        public async Task<PaginatedList<Lead>> GetLeadsAsync(
            UserContext userContext,
            string? searchTerm,
            string? statusFilter,
            string? sourceFilter,
            string? priorityFilter,
            string? assignedToFilter,
            string? sortOrder,
            int pageIndex,
            int pageSize)
        {
            var query = _context.Leads
                .Include(l => l.AssignedTo)
                .Include(l => l.Customer)
                .AsNoTracking()
                .AsQueryable();

            query = ApplyUserScope(query, userContext);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(l =>
                    l.LeadName.ToLower().Contains(term) ||
                    l.Email.ToLower().Contains(term) ||
                    l.Phone.ToLower().Contains(term) ||
                    (l.CompanyName != null && l.CompanyName.ToLower().Contains(term)) ||
                    l.LeadCode.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(l => l.Status == statusFilter);
            }

            if (!string.IsNullOrWhiteSpace(sourceFilter))
            {
                query = query.Where(l => l.Source == sourceFilter);
            }

            if (!string.IsNullOrWhiteSpace(priorityFilter))
            {
                query = query.Where(l => l.Priority == priorityFilter);
            }

            if (!string.IsNullOrWhiteSpace(assignedToFilter) && !userContext.IsSalesExecutive)
            {
                query = query.Where(l => l.AssignedToId == assignedToFilter);
            }

            query = sortOrder switch
            {
                "name_desc" => query.OrderByDescending(l => l.LeadName),
                "date_asc" => query.OrderBy(l => l.CreatedDate),
                "date_desc" => query.OrderByDescending(l => l.CreatedDate),
                "value_desc" => query.OrderByDescending(l => (double)l.ExpectedValue),
                "value_asc" => query.OrderBy(l => (double)l.ExpectedValue),
                _ => query.OrderByDescending(l => l.CreatedDate)
            };

            return await PaginatedList<Lead>.CreateAsync(query, pageIndex, pageSize);
        }

        public async Task<Lead?> GetLeadByIdAsync(int id, UserContext userContext)
        {
            var query = _context.Leads
                .Include(l => l.AssignedTo)
                .Include(l => l.Customer)
                .Include(l => l.Opportunities)
                .Include(l => l.FollowUps)
                .Include(l => l.Activities)
                .AsQueryable();

            var lead = await query.FirstOrDefaultAsync(l => l.LeadId == id);
            if (lead == null)
            {
                return null;
            }

            if (userContext.IsSalesExecutive && lead.AssignedToId != userContext.UserId)
            {
                throw new UnauthorizedAccessException("You are not authorized to view this lead.");
            }

            return lead;
        }

        public async Task<(bool Success, string Message, Lead? Lead)> CreateLeadAsync(Lead lead, UserContext userContext)
        {
            if (string.IsNullOrWhiteSpace(lead.LeadName))
            {
                return (false, "Lead name is required.", null);
            }

            if (lead.ExpectedValue < 0)
            {
                return (false, "Expected value must be numeric and greater than or equal to 0.", null);
            }

            if (string.IsNullOrWhiteSpace(lead.LeadCode))
            {
                var nextNum = (await _context.Leads.CountAsync()) + 2001;
                lead.LeadCode = $"LEAD-{nextNum}";
            }

            if (userContext.IsSalesExecutive)
            {
                lead.AssignedToId = userContext.UserId;
            }

            lead.CreatedDate = DateTime.UtcNow;
            lead.CreatedBy = userContext.UserEmail;

            _context.Leads.Add(lead);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userContext.UserId,
                userContext.UserEmail,
                "Create",
                "Lead",
                lead.LeadId.ToString(),
                null,
                $"Code: {lead.LeadCode}, Name: {lead.LeadName}, Source: {lead.Source}, Status: {lead.Status}, Value: {lead.ExpectedValue:C}",
                $"Created lead {lead.LeadName} ({lead.LeadCode})",
                userContext.IpAddress);

            return (true, "Lead created successfully.", lead);
        }

        public async Task<(bool Success, string Message, Lead? Lead)> UpdateLeadAsync(Lead lead, UserContext userContext)
        {
            var existing = await _context.Leads.FirstOrDefaultAsync(l => l.LeadId == lead.LeadId);
            if (existing == null)
            {
                return (false, "Lead not found.", null);
            }

            if (userContext.IsSalesExecutive && existing.AssignedToId != userContext.UserId)
            {
                throw new UnauthorizedAccessException("You are not authorized to modify this lead.");
            }

            if (string.IsNullOrWhiteSpace(lead.LeadName))
            {
                return (false, "Lead name is required.", null);
            }

            if (lead.ExpectedValue < 0)
            {
                return (false, "Expected value must be numeric and greater than or equal to 0.", null);
            }

            // Lead status transition audit
            var statusChanged = existing.Status != lead.Status;
            var oldSummary = $"Status: {existing.Status}, Value: {existing.ExpectedValue:C}, Priority: {existing.Priority}, Assigned: {existing.AssignedToId}";

            existing.LeadName = lead.LeadName;
            existing.Email = lead.Email;
            existing.Phone = lead.Phone;
            existing.CompanyName = lead.CompanyName;
            existing.Source = lead.Source;
            existing.Status = lead.Status;
            existing.Priority = lead.Priority;
            existing.ExpectedValue = lead.ExpectedValue;
            existing.Notes = lead.Notes;

            if (!userContext.IsSalesExecutive)
            {
                existing.AssignedToId = lead.AssignedToId;
            }

            await _context.SaveChangesAsync();

            var newSummary = $"Status: {existing.Status}, Value: {existing.ExpectedValue:C}, Priority: {existing.Priority}, Assigned: {existing.AssignedToId}";

            await _auditService.LogAsync(
                userContext.UserId,
                userContext.UserEmail,
                statusChanged ? "Status Change" : "Update",
                "Lead",
                existing.LeadId.ToString(),
                oldSummary,
                newSummary,
                statusChanged ? $"Changed lead status from {oldSummary} to {newSummary}" : $"Updated lead {existing.LeadName}",
                userContext.IpAddress);

            return (true, "Lead updated successfully.", existing);
        }

        public async Task<(bool Success, string Message)> DeleteOrDeactivateLeadAsync(int id, UserContext userContext)
        {
            var lead = await _context.Leads
                .Include(l => l.Opportunities)
                .Include(l => l.FollowUps)
                .FirstOrDefaultAsync(l => l.LeadId == id);

            if (lead == null)
            {
                return (false, "Lead not found.");
            }

            if (userContext.IsSalesExecutive && lead.AssignedToId != userContext.UserId)
            {
                throw new UnauthorizedAccessException("You are not authorized to modify this lead.");
            }

            if (lead.Opportunities.Any() || lead.Status == "Converted")
            {
                lead.Status = "Lost";
                await _context.SaveChangesAsync();

                await _auditService.LogAsync(
                    userContext.UserId,
                    userContext.UserEmail,
                    "Deactivation",
                    "Lead",
                    lead.LeadId.ToString(),
                    "Status: Active",
                    "Status: Lost",
                    $"Marked lead {lead.LeadName} as Lost due to historical records.",
                    userContext.IpAddress);

                return (true, "Lead has linked records and has been marked as Lost.");
            }

            _context.Leads.Remove(lead);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userContext.UserId,
                userContext.UserEmail,
                "Delete",
                "Lead",
                id.ToString(),
                $"Name: {lead.LeadName}, Code: {lead.LeadCode}",
                null,
                $"Deleted lead {lead.LeadName}",
                userContext.IpAddress);

            return (true, "Lead deleted successfully.");
        }

        public async Task<(bool Success, string Message, Customer? Customer, Opportunity? Opportunity)> ConvertLeadAsync(
            int leadId,
            ConvertLeadViewModel model,
            UserContext userContext)
        {
            var lead = await _context.Leads.FirstOrDefaultAsync(l => l.LeadId == leadId);
            if (lead == null)
            {
                return (false, "Lead not found.", null, null);
            }

            if (userContext.IsSalesExecutive && lead.AssignedToId != userContext.UserId)
            {
                throw new UnauthorizedAccessException("You are not authorized to convert this lead.");
            }

            if (lead.Status == "Converted")
            {
                return (false, "This lead has already been converted.", null, null);
            }

            // 1. Check or Create Customer from Lead
            var existingCustomer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Email.ToLower() == lead.Email.ToLower() || c.Phone == lead.Phone);

            Customer customer;
            if (existingCustomer != null)
            {
                customer = existingCustomer;
            }
            else
            {
                var custCount = await _context.Customers.CountAsync();
                customer = new Customer
                {
                    CustomerCode = $"CUST-{custCount + 1001}",
                    CustomerName = lead.LeadName,
                    CompanyName = lead.CompanyName,
                    Email = lead.Email,
                    Phone = lead.Phone,
                    Status = "Active",
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = userContext.UserEmail,
                    AssignedToId = lead.AssignedToId ?? userContext.UserId
                };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
            }

            // 2. Optionally create linked Opportunity
            Opportunity? opportunity = null;
            if (model.CreateOpportunity && model.OpportunityAmount > 0)
            {
                var oppCount = await _context.Opportunities.CountAsync();
                opportunity = new Opportunity
                {
                    OpportunityCode = $"OPP-{oppCount + 3001}",
                    OpportunityName = string.IsNullOrWhiteSpace(model.OpportunityName) ? $"{lead.CompanyName ?? lead.LeadName} - Deal" : model.OpportunityName,
                    CustomerId = customer.CustomerId,
                    LeadId = lead.LeadId,
                    Amount = model.OpportunityAmount,
                    Stage = "Qualification",
                    Probability = model.Probability,
                    ExpectedCloseDate = model.ExpectedCloseDate ?? DateTime.Today.AddDays(30),
                    Status = "Open",
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = userContext.UserEmail,
                    AssignedToId = lead.AssignedToId ?? userContext.UserId,
                    Description = $"Created via conversion from lead {lead.LeadCode}."
                };
                _context.Opportunities.Add(opportunity);
                await _context.SaveChangesAsync();
            }

            // 3. Update Lead status to Converted
            lead.Status = "Converted";
            lead.CustomerId = customer.CustomerId;
            lead.ConvertedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // 4. Audit conversion
            await _auditService.LogAsync(
                userContext.UserId,
                userContext.UserEmail,
                "Lead Conversion",
                "Lead",
                lead.LeadId.ToString(),
                $"Status: {lead.Status}",
                $"Converted -> Customer: {customer.CustomerName} (Id: {customer.CustomerId}), Opportunity: {opportunity?.OpportunityName ?? "None"}",
                $"Lead {lead.LeadName} ({lead.LeadCode}) converted successfully.",
                userContext.IpAddress);

            return (true, "Lead converted to Customer and Opportunity successfully.", customer, opportunity);
        }

        public async Task<List<Lead>> GetOpenLeadsForDropdownAsync(UserContext userContext)
        {
            var query = _context.Leads
                .Where(l => l.Status != "Converted" && l.Status != "Lost")
                .AsNoTracking();

            query = ApplyUserScope(query, userContext);
            return await query.OrderBy(l => l.LeadName).ToListAsync();
        }
    }
}
