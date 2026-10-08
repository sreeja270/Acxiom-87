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
    public class FollowUpsController : Controller
    {
        private readonly IFollowUpService _followUpService;
        private readonly ICustomerService _customerService;
        private readonly ILeadService _leadService;
        private readonly UserManager<ApplicationUser> _userManager;

        public FollowUpsController(
            IFollowUpService followUpService,
            ICustomerService customerService,
            ILeadService leadService,
            UserManager<ApplicationUser> userManager)
        {
            _followUpService = followUpService;
            _customerService = customerService;
            _leadService = leadService;
            _userManager = userManager;
        }

        private UserContext GetUserContext()
        {
            return UserContext.FromClaims(User, HttpContext.Connection.RemoteIpAddress?.ToString());
        }

        private async Task PopulateDropdownsAsync(FollowUpFormViewModel model, UserContext userContext)
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
            string? statusFilter,
            string? typeFilter,
            string? dateFilter,
            string? assignedToFilter,
            int pageIndex = 1)
        {
            var userContext = GetUserContext();
            var pageSize = 10;

            var followUps = await _followUpService.GetFollowUpsAsync(
                userContext,
                searchTerm,
                statusFilter,
                typeFilter,
                dateFilter,
                assignedToFilter,
                pageIndex,
                pageSize);

            var (overdue, today, upcoming) = await _followUpService.GetFollowUpCountersAsync(userContext);
            var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();

            var viewModel = new FollowUpListViewModel
            {
                FollowUps = followUps,
                SearchTerm = searchTerm,
                StatusFilter = statusFilter,
                TypeFilter = typeFilter,
                DateFilter = dateFilter,
                AssignedToFilter = assignedToFilter,
                PageIndex = pageIndex,
                OverdueCount = overdue,
                TodayCount = today,
                UpcomingCount = upcoming,
                UsersList = new SelectList(users, nameof(ApplicationUser.Id), nameof(ApplicationUser.FullName), assignedToFilter)
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? customerId = null, int? leadId = null)
        {
            var userContext = GetUserContext();
            var model = new FollowUpFormViewModel
            {
                CustomerId = customerId,
                LeadId = leadId,
                FollowUpDate = DateTime.Today.AddDays(1).AddHours(10),
                AssignedToId = userContext.UserId
            };

            await PopulateDropdownsAsync(model, userContext);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FollowUpFormViewModel model)
        {
            var userContext = GetUserContext();

            // Business rule: Planned follow-up date cannot be earlier than today
            if (model.Status == "Planned" && model.FollowUpDate.Date < DateTime.Today)
            {
                ModelState.AddModelError(nameof(model.FollowUpDate), "Follow-up date cannot be earlier than today.");
            }

            if (string.IsNullOrWhiteSpace(model.Remarks))
            {
                ModelState.AddModelError(nameof(model.Remarks), "Remarks are required.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync(model, userContext);
                return View(model);
            }

            var followUp = new FollowUp
            {
                CustomerId = model.CustomerId > 0 ? model.CustomerId : null,
                LeadId = model.LeadId > 0 ? model.LeadId : null,
                FollowUpDate = model.FollowUpDate,
                FollowUpType = model.FollowUpType,
                Remarks = model.Remarks,
                Status = model.Status,
                AssignedToId = string.IsNullOrWhiteSpace(model.AssignedToId) ? userContext.UserId : model.AssignedToId
            };

            var (success, message, _) = await _followUpService.CreateFollowUpAsync(followUp, userContext);
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
                var followUp = await _followUpService.GetFollowUpByIdAsync(id, userContext);
                if (followUp == null)
                {
                    return NotFound();
                }

                var model = new FollowUpFormViewModel
                {
                    FollowUpId = followUp.FollowUpId,
                    CustomerId = followUp.CustomerId,
                    LeadId = followUp.LeadId,
                    FollowUpDate = followUp.FollowUpDate,
                    FollowUpType = followUp.FollowUpType,
                    Remarks = followUp.Remarks,
                    Status = followUp.Status,
                    AssignedToId = followUp.AssignedToId,
                    CompletionNotes = followUp.CompletionNotes
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
        public async Task<IActionResult> Edit(FollowUpFormViewModel model)
        {
            var userContext = GetUserContext();

            if (string.IsNullOrWhiteSpace(model.Remarks))
            {
                ModelState.AddModelError(nameof(model.Remarks), "Remarks are required.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync(model, userContext);
                return View(model);
            }

            try
            {
                var followUp = new FollowUp
                {
                    FollowUpId = model.FollowUpId,
                    CustomerId = model.CustomerId > 0 ? model.CustomerId : null,
                    LeadId = model.LeadId > 0 ? model.LeadId : null,
                    FollowUpDate = model.FollowUpDate,
                    FollowUpType = model.FollowUpType,
                    Remarks = model.Remarks,
                    Status = model.Status,
                    AssignedToId = model.AssignedToId,
                    CompletionNotes = model.CompletionNotes
                };

                var (success, message, _) = await _followUpService.UpdateFollowUpAsync(followUp, userContext);
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
        public async Task<IActionResult> UpdateStatus(int id, string newStatus, string? notes, DateTime? newDate)
        {
            var userContext = GetUserContext();
            try
            {
                var (success, message) = await _followUpService.UpdateStatusAsync(id, newStatus, notes, newDate, userContext);
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userContext = GetUserContext();
            try
            {
                var (success, message) = await _followUpService.DeleteFollowUpAsync(id, userContext);
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
