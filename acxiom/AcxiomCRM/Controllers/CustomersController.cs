using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class CustomersController : Controller
    {
        private readonly ICustomerService _customerService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditService _auditService;

        public CustomersController(
            ICustomerService customerService,
            UserManager<ApplicationUser> userManager,
            IAuditService auditService)
        {
            _customerService = customerService;
            _userManager = userManager;
            _auditService = auditService;
        }

        private UserContext GetUserContext()
        {
            return UserContext.FromClaims(User, HttpContext.Connection.RemoteIpAddress?.ToString());
        }

        private async Task PopulateUsersDropdownAsync(CustomerFormViewModel model)
        {
            var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
            model.UsersList = new SelectList(users, nameof(ApplicationUser.Id), nameof(ApplicationUser.FullName), model.AssignedToId);
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? searchTerm,
            string? statusFilter,
            string? assignedToFilter,
            string? sortOrder,
            int pageIndex = 1)
        {
            var userContext = GetUserContext();
            var pageSize = 10;

            var customers = await _customerService.GetCustomersAsync(
                userContext,
                searchTerm,
                statusFilter,
                assignedToFilter,
                sortOrder,
                pageIndex,
                pageSize);

            var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();

            var viewModel = new CustomerListViewModel
            {
                Customers = customers,
                SearchTerm = searchTerm,
                StatusFilter = statusFilter,
                AssignedToFilter = assignedToFilter,
                SortOrder = sortOrder,
                PageIndex = pageIndex,
                UsersList = new SelectList(users, nameof(ApplicationUser.Id), nameof(ApplicationUser.FullName), assignedToFilter)
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userContext = GetUserContext();
            try
            {
                var customer = await _customerService.GetCustomerByIdAsync(id, userContext);
                if (customer == null)
                {
                    return NotFound();
                }

                var auditLogs = await _auditService.GetRecentLogsForEntityAsync("Customer", id.ToString());

                var vm = new CustomerDetailsViewModel
                {
                    Customer = customer,
                    RelatedLeads = customer.Leads.OrderByDescending(l => l.CreatedDate).ToList(),
                    RelatedOpportunities = customer.Opportunities.OrderByDescending(o => o.CreatedDate).ToList(),
                    RelatedFollowUps = customer.FollowUps.OrderByDescending(f => f.FollowUpDate).ToList(),
                    RelatedActivities = customer.Activities.OrderByDescending(a => a.ActivityDate).ToList(),
                    AuditHistory = auditLogs,
                    TotalOpportunityValue = customer.Opportunities.Sum(o => o.Amount),
                    TotalWonValue = customer.Opportunities.Where(o => o.Stage == "Won").Sum(o => o.Amount)
                };

                return View(vm);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new CustomerFormViewModel();
            await PopulateUsersDropdownAsync(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerFormViewModel model)
        {
            var userContext = GetUserContext();

            // Server-side validations
            if (string.IsNullOrWhiteSpace(model.CustomerName))
            {
                ModelState.AddModelError(nameof(model.CustomerName), "Customer Name is required.");
            }

            if (!string.IsNullOrWhiteSpace(model.Email) && !await _customerService.IsEmailUniqueAsync(model.Email))
            {
                ModelState.AddModelError(nameof(model.Email), "Email already exists.");
            }

            if (!string.IsNullOrWhiteSpace(model.Phone) && !await _customerService.IsPhoneUniqueAsync(model.Phone))
            {
                ModelState.AddModelError(nameof(model.Phone), "Phone number already exists.");
            }

            if (await _customerService.IsDuplicateCustomerAsync(model.CustomerName, model.CompanyName))
            {
                ModelState.AddModelError(string.Empty, "Duplicate customer cannot be created.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateUsersDropdownAsync(model);
                return View(model);
            }

            var customer = new Customer
            {
                CustomerName = model.CustomerName,
                Email = model.Email,
                Phone = model.Phone,
                CompanyName = model.CompanyName,
                Address = model.Address,
                City = model.City,
                State = model.State,
                Status = model.Status,
                AssignedToId = model.AssignedToId
            };

            var (success, message, created) = await _customerService.CreateCustomerAsync(customer, userContext);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                await PopulateUsersDropdownAsync(model);
                return View(model);
            }

            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Details), new { id = created!.CustomerId });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userContext = GetUserContext();
            try
            {
                var customer = await _customerService.GetCustomerByIdAsync(id, userContext);
                if (customer == null)
                {
                    return NotFound();
                }

                var model = new CustomerFormViewModel
                {
                    CustomerId = customer.CustomerId,
                    CustomerCode = customer.CustomerCode,
                    CustomerName = customer.CustomerName,
                    Email = customer.Email,
                    Phone = customer.Phone,
                    CompanyName = customer.CompanyName,
                    Address = customer.Address,
                    City = customer.City,
                    State = customer.State,
                    Status = customer.Status,
                    AssignedToId = customer.AssignedToId
                };

                await PopulateUsersDropdownAsync(model);
                return View(model);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CustomerFormViewModel model)
        {
            var userContext = GetUserContext();

            if (string.IsNullOrWhiteSpace(model.CustomerName))
            {
                ModelState.AddModelError(nameof(model.CustomerName), "Customer Name is required.");
            }

            if (!string.IsNullOrWhiteSpace(model.Email) && !await _customerService.IsEmailUniqueAsync(model.Email, model.CustomerId))
            {
                ModelState.AddModelError(nameof(model.Email), "Email already exists.");
            }

            if (!string.IsNullOrWhiteSpace(model.Phone) && !await _customerService.IsPhoneUniqueAsync(model.Phone, model.CustomerId))
            {
                ModelState.AddModelError(nameof(model.Phone), "Phone number already exists.");
            }

            if (await _customerService.IsDuplicateCustomerAsync(model.CustomerName, model.CompanyName, model.CustomerId))
            {
                ModelState.AddModelError(string.Empty, "Duplicate customer cannot be created.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateUsersDropdownAsync(model);
                return View(model);
            }

            try
            {
                var customer = new Customer
                {
                    CustomerId = model.CustomerId,
                    CustomerCode = model.CustomerCode ?? string.Empty,
                    CustomerName = model.CustomerName,
                    Email = model.Email,
                    Phone = model.Phone,
                    CompanyName = model.CompanyName,
                    Address = model.Address,
                    City = model.City,
                    State = model.State,
                    Status = model.Status,
                    AssignedToId = model.AssignedToId
                };

                var (success, message, _) = await _customerService.UpdateCustomerAsync(customer, userContext);
                if (!success)
                {
                    ModelState.AddModelError(string.Empty, message);
                    await PopulateUsersDropdownAsync(model);
                    return View(model);
                }

                TempData["SuccessMessage"] = message;
                return RedirectToAction(nameof(Details), new { id = model.CustomerId });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userContext = GetUserContext();
            try
            {
                var (success, message) = await _customerService.DeleteOrDeactivateCustomerAsync(id, userContext);
                if (success)
                {
                    TempData["SuccessMessage"] = message;
                }
                else
                {
                    TempData["ErrorMessage"] = message;
                }
                return RedirectToAction(nameof(Index));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }
    }
}
