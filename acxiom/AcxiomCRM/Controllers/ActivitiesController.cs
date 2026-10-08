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
    public class ActivitiesController : Controller
    {
        private readonly IActivityService _activityService;
        private readonly ICustomerService _customerService;
        private readonly ILeadService _leadService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ActivitiesController(
            IActivityService activityService,
            ICustomerService customerService,
            ILeadService leadService,
            UserManager<ApplicationUser> userManager)
        {
            _activityService = activityService;
            _customerService = customerService;
            _leadService = leadService;
            _userManager = userManager;
        }

        private UserContext GetUserContext()
        {
            return UserContext.FromClaims(User, HttpContext.Connection.RemoteIpAddress?.ToString());
        }

        private async Task PopulateDropdownsAsync(ActivityFormViewModel model, UserContext userContext)
        {
            var customers = await _customerService.GetActiveCustomersForDropdownAsync(userContext);
            var leads = await _leadService.GetOpenLeadsForDropdownAsync(userContext);
            var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();

            model.CustomersList = new SelectList(customers, nameof(Customer.CustomerId), nameof(Customer.CustomerName), model.CustomerId);
            model.LeadsList = new SelectList(leads, nameof(Lead.LeadId), nameof(Lead.LeadName), model.LeadId);
            model.UsersList = new SelectList(users, nameof(ApplicationUser.Id), nameof(ApplicationUser.FullName), model.AssignedToId);
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? searchTerm,
            string? typeFilter,
            string? statusFilter,
            string? assignedToFilter,
            int pageIndex = 1)
        {
            var userContext = GetUserContext();
            var pageSize = 10;

            var activities = await _activityService.GetActivitiesAsync(
                userContext,
                searchTerm,
                typeFilter,
                statusFilter,
                assignedToFilter,
                pageIndex,
                pageSize);

            var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();

            var viewModel = new ActivityListViewModel
            {
                Activities = activities,
                SearchTerm = searchTerm,
                TypeFilter = typeFilter,
                StatusFilter = statusFilter,
                AssignedToFilter = assignedToFilter,
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
                var activity = await _activityService.GetActivityByIdAsync(id, userContext);
                if (activity == null)
                {
                    return NotFound();
                }

                return View(activity);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? customerId = null, int? leadId = null)
        {
            var userContext = GetUserContext();
            var model = new ActivityFormViewModel
            {
                CustomerId = customerId,
                LeadId = leadId,
                ActivityDate = DateTime.UtcNow,
                AssignedToId = userContext.UserId
            };

            await PopulateDropdownsAsync(model, userContext);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ActivityFormViewModel model)
        {
            var userContext = GetUserContext();

            if (string.IsNullOrWhiteSpace(model.Subject))
            {
                ModelState.AddModelError(nameof(model.Subject), "Subject is required.");
            }

            if (string.IsNullOrWhiteSpace(model.Description))
            {
                ModelState.AddModelError(nameof(model.Description), "Description is required.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync(model, userContext);
                return View(model);
            }

            var activity = new Activity
            {
                ActivityType = model.ActivityType,
                Subject = model.Subject,
                Description = model.Description,
                ActivityDate = model.ActivityDate,
                CustomerId = model.CustomerId > 0 ? model.CustomerId : null,
                LeadId = model.LeadId > 0 ? model.LeadId : null,
                Status = model.Status,
                AssignedToId = string.IsNullOrWhiteSpace(model.AssignedToId) ? userContext.UserId : model.AssignedToId
            };

            var (success, message, _) = await _activityService.CreateActivityAsync(activity, userContext);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                await PopulateDropdownsAsync(model, userContext);
                return View(model);
            }

            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userContext = GetUserContext();
            try
            {
                var act = await _activityService.GetActivityByIdAsync(id, userContext);
                if (act == null)
                {
                    return NotFound();
                }

                var model = new ActivityFormViewModel
                {
                    ActivityId = act.ActivityId,
                    ActivityType = act.ActivityType,
                    Subject = act.Subject,
                    Description = act.Description,
                    ActivityDate = act.ActivityDate,
                    CustomerId = act.CustomerId,
                    LeadId = act.LeadId,
                    Status = act.Status,
                    AssignedToId = act.AssignedToId
                };

                await PopulateDropdownsAsync(model, userContext);
                return View(model);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ActivityFormViewModel model)
        {
            var userContext = GetUserContext();

            if (string.IsNullOrWhiteSpace(model.Subject))
            {
                ModelState.AddModelError(nameof(model.Subject), "Subject is required.");
            }

            if (string.IsNullOrWhiteSpace(model.Description))
            {
                ModelState.AddModelError(nameof(model.Description), "Description is required.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync(model, userContext);
                return View(model);
            }

            try
            {
                var act = new Activity
                {
                    ActivityId = model.ActivityId,
                    ActivityType = model.ActivityType,
                    Subject = model.Subject,
                    Description = model.Description,
                    ActivityDate = model.ActivityDate,
                    CustomerId = model.CustomerId > 0 ? model.CustomerId : null,
                    LeadId = model.LeadId > 0 ? model.LeadId : null,
                    Status = model.Status,
                    AssignedToId = model.AssignedToId
                };

                var (success, message, _) = await _activityService.UpdateActivityAsync(act, userContext);
                if (!success)
                {
                    ModelState.AddModelError(string.Empty, message);
                    await PopulateDropdownsAsync(model, userContext);
                    return View(model);
                }

                TempData["SuccessMessage"] = message;
                return RedirectToAction(nameof(Index));
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
                var (success, message) = await _activityService.DeleteActivityAsync(id, userContext);
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
