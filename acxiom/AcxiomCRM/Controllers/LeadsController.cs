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
    public class LeadsController : Controller
    {
        private readonly ILeadService _leadService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditService _auditService;

        public LeadsController(
            ILeadService leadService,
            UserManager<ApplicationUser> userManager,
            IAuditService auditService)
        {
            _leadService = leadService;
            _userManager = userManager;
            _auditService = auditService;
        }

        private UserContext GetUserContext()
        {
            return UserContext.FromClaims(User, HttpContext.Connection.RemoteIpAddress?.ToString());
        }

        private async Task PopulateUsersDropdownAsync(LeadFormViewModel model)
        {
            var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
            model.UsersList = new SelectList(users, nameof(ApplicationUser.Id), nameof(ApplicationUser.FullName), model.AssignedToId);
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? searchTerm,
            string? statusFilter,
            string? sourceFilter,
            string? priorityFilter,
            string? assignedToFilter,
            string? sortOrder,
            int pageIndex = 1)
        {
            var userContext = GetUserContext();
            var pageSize = 10;

            var leads = await _leadService.GetLeadsAsync(
                userContext,
                searchTerm,
                statusFilter,
                sourceFilter,
                priorityFilter,
                assignedToFilter,
                sortOrder,
                pageIndex,
                pageSize);

            var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();

            var viewModel = new LeadListViewModel
            {
                Leads = leads,
                SearchTerm = searchTerm,
                StatusFilter = statusFilter,
                SourceFilter = sourceFilter,
                PriorityFilter = priorityFilter,
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
                var lead = await _leadService.GetLeadByIdAsync(id, userContext);
                if (lead == null)
                {
                    return NotFound();
                }

                var auditLogs = await _auditService.GetRecentLogsForEntityAsync("Lead", id.ToString());

                var vm = new LeadDetailsViewModel
                {
                    Lead = lead,
                    ConvertedCustomer = lead.Customer,
                    RelatedOpportunities = lead.Opportunities.OrderByDescending(o => o.CreatedDate).ToList(),
                    RelatedFollowUps = lead.FollowUps.OrderByDescending(f => f.FollowUpDate).ToList(),
                    RelatedActivities = lead.Activities.OrderByDescending(a => a.ActivityDate).ToList(),
                    AuditHistory = auditLogs
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
            var model = new LeadFormViewModel();
            await PopulateUsersDropdownAsync(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LeadFormViewModel model)
        {
            var userContext = GetUserContext();

            if (string.IsNullOrWhiteSpace(model.LeadName))
            {
                ModelState.AddModelError(nameof(model.LeadName), "Lead name is required.");
            }

            if (model.ExpectedValue < 0)
            {
                ModelState.AddModelError(nameof(model.ExpectedValue), "Expected value must be numeric and greater than or equal to 0.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateUsersDropdownAsync(model);
                return View(model);
            }

            var lead = new Lead
            {
                LeadName = model.LeadName,
                Email = model.Email,
                Phone = model.Phone,
                CompanyName = model.CompanyName,
                Source = model.Source,
                Status = model.Status,
                Priority = model.Priority,
                ExpectedValue = model.ExpectedValue,
                AssignedToId = model.AssignedToId,
                Notes = model.Notes
            };

            var (success, message, created) = await _leadService.CreateLeadAsync(lead, userContext);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                await PopulateUsersDropdownAsync(model);
                return View(model);
            }

            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Details), new { id = created!.LeadId });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userContext = GetUserContext();
            try
            {
                var lead = await _leadService.GetLeadByIdAsync(id, userContext);
                if (lead == null)
                {
                    return NotFound();
                }

                var model = new LeadFormViewModel
                {
                    LeadId = lead.LeadId,
                    LeadCode = lead.LeadCode,
                    LeadName = lead.LeadName,
                    Email = lead.Email,
                    Phone = lead.Phone,
                    CompanyName = lead.CompanyName,
                    Source = lead.Source,
                    Status = lead.Status,
                    Priority = lead.Priority,
                    ExpectedValue = lead.ExpectedValue,
                    AssignedToId = lead.AssignedToId,
                    Notes = lead.Notes
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
        public async Task<IActionResult> Edit(LeadFormViewModel model)
        {
            var userContext = GetUserContext();

            if (string.IsNullOrWhiteSpace(model.LeadName))
            {
                ModelState.AddModelError(nameof(model.LeadName), "Lead name is required.");
            }

            if (model.ExpectedValue < 0)
            {
                ModelState.AddModelError(nameof(model.ExpectedValue), "Expected value must be numeric and greater than or equal to 0.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateUsersDropdownAsync(model);
                return View(model);
            }

            try
            {
                var lead = new Lead
                {
                    LeadId = model.LeadId,
                    LeadCode = model.LeadCode ?? string.Empty,
                    LeadName = model.LeadName,
                    Email = model.Email,
                    Phone = model.Phone,
                    CompanyName = model.CompanyName,
                    Source = model.Source,
                    Status = model.Status,
                    Priority = model.Priority,
                    ExpectedValue = model.ExpectedValue,
                    AssignedToId = model.AssignedToId,
                    Notes = model.Notes
                };

                var (success, message, _) = await _leadService.UpdateLeadAsync(lead, userContext);
                if (!success)
                {
                    ModelState.AddModelError(string.Empty, message);
                    await PopulateUsersDropdownAsync(model);
                    return View(model);
                }

                TempData["SuccessMessage"] = message;
                return RedirectToAction(nameof(Details), new { id = model.LeadId });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpGet]
        public async Task<IActionResult> Convert(int id)
        {
            var userContext = GetUserContext();
            try
            {
                var lead = await _leadService.GetLeadByIdAsync(id, userContext);
                if (lead == null)
                {
                    return NotFound();
                }

                if (lead.Status == "Converted")
                {
                    TempData["ErrorMessage"] = "This lead is already converted.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();

                var vm = new ConvertLeadViewModel
                {
                    LeadId = lead.LeadId,
                    LeadCode = lead.LeadCode,
                    LeadName = lead.LeadName,
                    Email = lead.Email,
                    Phone = lead.Phone,
                    CompanyName = lead.CompanyName,
                    CreateOpportunity = true,
                    OpportunityName = $"{lead.CompanyName ?? lead.LeadName} - Expansion Deal",
                    OpportunityAmount = lead.ExpectedValue > 0 ? lead.ExpectedValue : 10000m,
                    ExpectedCloseDate = DateTime.Today.AddDays(30),
                    Probability = 40,
                    AssignedToId = lead.AssignedToId,
                    UsersList = new SelectList(users, nameof(ApplicationUser.Id), nameof(ApplicationUser.FullName), lead.AssignedToId)
                };

                return View(vm);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Convert(ConvertLeadViewModel model)
        {
            var userContext = GetUserContext();

            if (model.CreateOpportunity && model.OpportunityAmount <= 0)
            {
                ModelState.AddModelError(nameof(model.OpportunityAmount), "Opportunity Amount must be greater than 0.");
            }

            if (model.Probability < 0 || model.Probability > 100)
            {
                ModelState.AddModelError(nameof(model.Probability), "Probability must be between 0 and 100.");
            }

            if (!ModelState.IsValid)
            {
                var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
                model.UsersList = new SelectList(users, nameof(ApplicationUser.Id), nameof(ApplicationUser.FullName), model.AssignedToId);
                return View(model);
            }

            try
            {
                var (success, message, customer, opportunity) = await _leadService.ConvertLeadAsync(model.LeadId, model, userContext);
                if (!success)
                {
                    TempData["ErrorMessage"] = message;
                    return RedirectToAction(nameof(Details), new { id = model.LeadId });
                }

                TempData["SuccessMessage"] = message;
                return RedirectToAction("Details", "Customers", new { id = customer!.CustomerId });
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
                var (success, message) = await _leadService.DeleteOrDeactivateLeadAsync(id, userContext);
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
