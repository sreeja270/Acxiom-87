using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.ViewModels.Common;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services
{
    public interface ICustomerService
    {
        Task<PaginatedList<Customer>> GetCustomersAsync(UserContext userContext, string? searchTerm, string? statusFilter, string? assignedToFilter, string? sortOrder, int pageIndex, int pageSize);
        Task<Customer?> GetCustomerByIdAsync(int id, UserContext userContext);
        Task<(bool Success, string Message, Customer? Customer)> CreateCustomerAsync(Customer customer, UserContext userContext);
        Task<(bool Success, string Message, Customer? Customer)> UpdateCustomerAsync(Customer customer, UserContext userContext);
        Task<(bool Success, string Message)> DeleteOrDeactivateCustomerAsync(int id, UserContext userContext);
        Task<bool> IsEmailUniqueAsync(string email, int currentCustomerId = 0);
        Task<bool> IsPhoneUniqueAsync(string phone, int currentCustomerId = 0);
        Task<bool> IsDuplicateCustomerAsync(string name, string? company, int currentCustomerId = 0);
        Task<List<Customer>> GetActiveCustomersForDropdownAsync(UserContext userContext);
    }

    public class CustomerService : ICustomerService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly ILogger<CustomerService> _logger;

        public CustomerService(ApplicationDbContext context, IAuditService auditService, ILogger<CustomerService> logger)
        {
            _context = context;
            _auditService = auditService;
            _logger = logger;
        }

        private IQueryable<Customer> ApplyUserScope(IQueryable<Customer> query, UserContext userContext)
        {
            if (userContext.IsSalesExecutive)
            {
                return query.Where(c => c.AssignedToId == userContext.UserId);
            }
            return query;
        }

        public async Task<PaginatedList<Customer>> GetCustomersAsync(
            UserContext userContext,
            string? searchTerm,
            string? statusFilter,
            string? assignedToFilter,
            string? sortOrder,
            int pageIndex,
            int pageSize)
        {
            var query = _context.Customers
                .Include(c => c.AssignedTo)
                .Include(c => c.Opportunities)
                .AsNoTracking()
                .AsQueryable();

            query = ApplyUserScope(query, userContext);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(c =>
                    c.CustomerName.ToLower().Contains(term) ||
                    c.Email.ToLower().Contains(term) ||
                    c.Phone.ToLower().Contains(term) ||
                    (c.CompanyName != null && c.CompanyName.ToLower().Contains(term)) ||
                    c.CustomerCode.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(c => c.Status == statusFilter);
            }

            if (!string.IsNullOrWhiteSpace(assignedToFilter) && !userContext.IsSalesExecutive)
            {
                query = query.Where(c => c.AssignedToId == assignedToFilter);
            }

            query = sortOrder switch
            {
                "name_desc" => query.OrderByDescending(c => c.CustomerName),
                "date_asc" => query.OrderBy(c => c.CreatedDate),
                "date_desc" => query.OrderByDescending(c => c.CreatedDate),
                "company" => query.OrderBy(c => c.CompanyName),
                _ => query.OrderBy(c => c.CustomerName)
            };

            return await PaginatedList<Customer>.CreateAsync(query, pageIndex, pageSize);
        }

        public async Task<Customer?> GetCustomerByIdAsync(int id, UserContext userContext)
        {
            var query = _context.Customers
                .Include(c => c.AssignedTo)
                .Include(c => c.Leads)
                .Include(c => c.Opportunities)
                .Include(c => c.FollowUps)
                .Include(c => c.Activities)
                .AsQueryable();

            var customer = await query.FirstOrDefaultAsync(c => c.CustomerId == id);
            if (customer == null)
            {
                return null;
            }

            // Scope check: If Sales Executive, verify ownership
            if (userContext.IsSalesExecutive && customer.AssignedToId != userContext.UserId)
            {
                throw new UnauthorizedAccessException("You are not authorized to view this customer.");
            }

            return customer;
        }

        public async Task<(bool Success, string Message, Customer? Customer)> CreateCustomerAsync(Customer customer, UserContext userContext)
        {
            // 1. Validation: Name
            if (string.IsNullOrWhiteSpace(customer.CustomerName))
            {
                return (false, "Customer Name is required.", null);
            }

            // 2. Validation: Email uniqueness
            if (string.IsNullOrWhiteSpace(customer.Email))
            {
                return (false, "Enter a valid email address.", null);
            }
            if (!await IsEmailUniqueAsync(customer.Email))
            {
                return (false, "Email already exists.", null);
            }

            // 3. Validation: Phone uniqueness
            if (string.IsNullOrWhiteSpace(customer.Phone))
            {
                return (false, "Enter a valid phone number.", null);
            }
            if (!await IsPhoneUniqueAsync(customer.Phone))
            {
                return (false, "Phone number already exists.", null);
            }

            // 4. Duplicate customer check
            if (await IsDuplicateCustomerAsync(customer.CustomerName, customer.CompanyName))
            {
                return (false, "Duplicate customer cannot be created.", null);
            }

            // Generate code if not set
            if (string.IsNullOrWhiteSpace(customer.CustomerCode))
            {
                var nextNum = (await _context.Customers.CountAsync()) + 1001;
                customer.CustomerCode = $"CUST-{nextNum}";
            }

            // Default assignment for sales executive
            if (userContext.IsSalesExecutive)
            {
                customer.AssignedToId = userContext.UserId;
            }

            customer.CreatedDate = DateTime.UtcNow;
            customer.CreatedBy = userContext.UserEmail;

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userContext.UserId,
                userContext.UserEmail,
                "Create",
                "Customer",
                customer.CustomerId.ToString(),
                null,
                $"Code: {customer.CustomerCode}, Name: {customer.CustomerName}, Email: {customer.Email}",
                $"Created customer {customer.CustomerName} ({customer.CustomerCode})",
                userContext.IpAddress);

            return (true, "Customer created successfully.", customer);
        }

        public async Task<(bool Success, string Message, Customer? Customer)> UpdateCustomerAsync(Customer customer, UserContext userContext)
        {
            var existing = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerId == customer.CustomerId);
            if (existing == null)
            {
                return (false, "Customer not found.", null);
            }

            // Scope check: If Sales Executive, verify assignment
            if (userContext.IsSalesExecutive && existing.AssignedToId != userContext.UserId)
            {
                throw new UnauthorizedAccessException("You are not authorized to modify this customer.");
            }

            if (string.IsNullOrWhiteSpace(customer.CustomerName))
            {
                return (false, "Customer Name is required.", null);
            }

            if (!await IsEmailUniqueAsync(customer.Email, customer.CustomerId))
            {
                return (false, "Email already exists.", null);
            }

            if (!await IsPhoneUniqueAsync(customer.Phone, customer.CustomerId))
            {
                return (false, "Phone number already exists.", null);
            }

            if (await IsDuplicateCustomerAsync(customer.CustomerName, customer.CompanyName, customer.CustomerId))
            {
                return (false, "Duplicate customer cannot be created.", null);
            }

            var oldSummary = $"Name: {existing.CustomerName}, Email: {existing.Email}, Phone: {existing.Phone}, Status: {existing.Status}, Assigned: {existing.AssignedToId}";

            existing.CustomerName = customer.CustomerName;
            existing.Email = customer.Email;
            existing.Phone = customer.Phone;
            existing.CompanyName = customer.CompanyName;
            existing.Address = customer.Address;
            existing.City = customer.City;
            existing.State = customer.State;
            existing.Status = customer.Status;

            // Only Admin / Manager can reassign customer
            if (!userContext.IsSalesExecutive)
            {
                existing.AssignedToId = customer.AssignedToId;
            }

            await _context.SaveChangesAsync();

            var newSummary = $"Name: {existing.CustomerName}, Email: {existing.Email}, Phone: {existing.Phone}, Status: {existing.Status}, Assigned: {existing.AssignedToId}";

            await _auditService.LogAsync(
                userContext.UserId,
                userContext.UserEmail,
                "Update",
                "Customer",
                existing.CustomerId.ToString(),
                oldSummary,
                newSummary,
                $"Updated customer {existing.CustomerName}",
                userContext.IpAddress);

            return (true, "Customer updated successfully.", existing);
        }

        public async Task<(bool Success, string Message)> DeleteOrDeactivateCustomerAsync(int id, UserContext userContext)
        {
            var customer = await _context.Customers
                .Include(c => c.Opportunities)
                .Include(c => c.Leads)
                .FirstOrDefaultAsync(c => c.CustomerId == id);

            if (customer == null)
            {
                return (false, "Customer not found.");
            }

            if (userContext.IsSalesExecutive && customer.AssignedToId != userContext.UserId)
            {
                throw new UnauthorizedAccessException("You are not authorized to modify this customer.");
            }

            // If customer has related opportunities or leads, deactivate rather than hard delete to preserve historical integrity
            if (customer.Opportunities.Any() || customer.Leads.Any())
            {
                customer.Status = "Inactive";
                await _context.SaveChangesAsync();

                await _auditService.LogAsync(
                    userContext.UserId,
                    userContext.UserEmail,
                    "Deactivation",
                    "Customer",
                    customer.CustomerId.ToString(),
                    "Status: Active",
                    "Status: Inactive",
                    $"Deactivated customer {customer.CustomerName} due to existing linked records.",
                    userContext.IpAddress);

                return (true, "Customer has existing linked records and has been deactivated to preserve CRM history.");
            }

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userContext.UserId,
                userContext.UserEmail,
                "Delete",
                "Customer",
                id.ToString(),
                $"Name: {customer.CustomerName}, Code: {customer.CustomerCode}",
                null,
                $"Deleted customer {customer.CustomerName}",
                userContext.IpAddress);

            return (true, "Customer deleted successfully.");
        }

        public async Task<bool> IsEmailUniqueAsync(string email, int currentCustomerId = 0)
        {
            var trimmed = email.Trim().ToLower();
            return !await _context.Customers.AnyAsync(c => c.Email.ToLower() == trimmed && c.CustomerId != currentCustomerId);
        }

        public async Task<bool> IsPhoneUniqueAsync(string phone, int currentCustomerId = 0)
        {
            var trimmed = phone.Trim();
            return !await _context.Customers.AnyAsync(c => c.Phone == trimmed && c.CustomerId != currentCustomerId);
        }

        public async Task<bool> IsDuplicateCustomerAsync(string name, string? company, int currentCustomerId = 0)
        {
            var nameTrimmed = name.Trim().ToLower();
            var companyTrimmed = (company ?? string.Empty).Trim().ToLower();

            if (!string.IsNullOrEmpty(companyTrimmed))
            {
                return await _context.Customers.AnyAsync(c =>
                    c.CustomerId != currentCustomerId &&
                    c.CustomerName.ToLower() == nameTrimmed &&
                    c.CompanyName != null && c.CompanyName.ToLower() == companyTrimmed);
            }

            return false;
        }

        public async Task<List<Customer>> GetActiveCustomersForDropdownAsync(UserContext userContext)
        {
            var query = _context.Customers.Where(c => c.Status == "Active").AsNoTracking();
            query = ApplyUserScope(query, userContext);
            return await query.OrderBy(c => c.CustomerName).ToListAsync();
        }
    }
}
