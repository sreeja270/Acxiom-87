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
    public class OpportunitiesController : Controller
    {
        private readonly IOpportunityService _opportunityService;
        private readonly ICustomerService _customerService;
        private readonly ILeadService _leadService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditService _auditService;

        public OpportunitiesController(
            IOpportunityService opportunityService,
            ICustomerService customerService,
            ILeadService leadService,
            UserManager<ApplicationUser> userManager,
            IAuditService auditService)
        {
            _opportunityService = opportunityService;
            _customerService = customerService;
            _leadService = leadService;
            _userManager = userManager;
            _auditService = auditService;
        }

        private UserContext GetUserContext()
        {
            return UserContext.FromClaims(User, HttpContext.Connection.RemoteIpAddress?.ToString());
        }

        private async Task PopulateDropdownsAsync(OpportunityFormViewModel model, UserContext userContext)
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
            string? stageFilter,
            string? statusFilter,
            string? assignedToFilter,
            string? sortOrder,
            int pageIndex = 1)
        {
            var userContext = GetUserContext();
            var pageSize = 10;

            var opportunities = await _opportunityService.GetOpportunitiesAsync(
                userContext,
                searchTerm,
                stageFilter,
                statusFilter,
                assignedToFilter,
                sortOrder,
                pageIndex,
                pageSize);

            var (total, weighted) = await _opportunityService.GetPipelineTotalsAsync(userContext);
            var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();

            var viewModel = new OpportunityListViewModel
            {
                Opportunities = opportunities,
                SearchTerm = searchTerm,
                StageFilter = stageFilter,
                StatusFilter = statusFilter,
                AssignedToFilter = assignedToFilter,
                SortOrder = sortOrder,
                PageIndex = pageIndex,
                TotalPipelineValue = total,
                TotalWeightedPipelineValue = weighted,
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
                var opportunity = await _opportunityService.GetOpportunityByIdAsync(id, userContext);
                if (opportunity == null)
                {
                    return NotFound();
                }

                var auditLogs = await _auditService.GetRecentLogsForEntityAsync("Opportunity", id.ToString());

                var vm = new OpportunityDetailsViewModel
                {
                    Opportunity = opportunity,
                    RelatedFollowUps = new List<FollowUp>(),
                    RelatedActivities = new List<Activity>(),
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
        public async Task<IActionResult> Create(int? customerId = null, int? leadId = null)
        {
            var userContext = GetUserContext();
            var model = new OpportunityFormViewModel
            {
                CustomerId = customerId ?? 0,
                LeadId = leadId,
                ExpectedCloseDate = DateTime.Today.AddDays(30)
            };

            await PopulateDropdownsAsync(model, userContext);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OpportunityFormViewModel model)
        {
            var userContext = GetUserContext();

            // Business Rules Server Validation
            if (model.Amount <= 0)
            {
                ModelState.AddModelError(nameof(model.Amount), "Opportunity Amount must be greater than 0.");
            }

            if (model.Probability < 0 || model.Probability > 100)
            {
                ModelState.AddModelError(nameof(model.Probability), "Probability must be between 0 and 100.");
            }

            if (model.Status == "Open" && model.ExpectedCloseDate.Date < DateTime.Today)
            {
                ModelState.AddModelError(nameof(model.ExpectedCloseDate), "Expected Close Date cannot be in the past.");
            }

            if (model.CustomerId <= 0)
            {
                ModelState.AddModelError(nameof(model.CustomerId), "Customer is required.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync(model, userContext);
                return View(model);
            }

            var opp = new Opportunity
            {
                OpportunityName = model.OpportunityName,
                CustomerId = model.CustomerId,
                LeadId = model.LeadId > 0 ? model.LeadId : null,
                Amount = model.Amount,
                Stage = model.Stage,
                Probability = model.Probability,
                ExpectedCloseDate = model.ExpectedCloseDate,
                Status = model.Status,
                AssignedToId = model.AssignedToId,
                Description = model.Description
            };

            var (success, message, created) = await _opportunityService.CreateOpportunityAsync(opp, userContext);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                await PopulateDropdownsAsync(model, userContext);
                return View(model);
            }

            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Details), new { id = created!.OpportunityId });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userContext = GetUserContext();
            try
            {
                var opp = await _opportunityService.GetOpportunityByIdAsync(id, userContext);
                if (opp == null)
                {
                    return NotFound();
                }

                var model = new OpportunityFormViewModel
                {
                    OpportunityId = opp.OpportunityId,
                    OpportunityCode = opp.OpportunityCode,
                    OpportunityName = opp.OpportunityName,
                    CustomerId = opp.CustomerId,
                    LeadId = opp.LeadId,
                    Amount = opp.Amount,
                    Stage = opp.Stage,
                    Probability = opp.Probability,
                    ExpectedCloseDate = opp.ExpectedCloseDate,
                    Status = opp.Status,
                    AssignedToId = opp.AssignedToId,
                    Description = opp.Description
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
        public async Task<IActionResult> Edit(OpportunityFormViewModel model)
        {
            var userContext = GetUserContext();

            if (model.Amount <= 0)
            {
                ModelState.AddModelError(nameof(model.Amount), "Opportunity Amount must be greater than 0.");
            }

            if (model.Probability < 0 || model.Probability > 100)
            {
                ModelState.AddModelError(nameof(model.Probability), "Probability must be between 0 and 100.");
            }

            if (model.Status == "Open" && model.ExpectedCloseDate.Date < DateTime.Today)
            {
                ModelState.AddModelError(nameof(model.ExpectedCloseDate), "Expected Close Date cannot be in the past.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync(model, userContext);
                return View(model);
            }

            try
            {
                var opp = new Opportunity
                {
                    OpportunityId = model.OpportunityId,
                    OpportunityCode = model.OpportunityCode ?? string.Empty,
                    OpportunityName = model.OpportunityName,
                    CustomerId = model.CustomerId,
                    LeadId = model.LeadId > 0 ? model.LeadId : null,
                    Amount = model.Amount,
                    Stage = model.Stage,
                    Probability = model.Probability,
                    ExpectedCloseDate = model.ExpectedCloseDate,
                    Status = model.Status,
                    AssignedToId = model.AssignedToId,
                    Description = model.Description
                };

                var (success, message, _) = await _opportunityService.UpdateOpportunityAsync(opp, userContext);
                if (!success)
                {
                    ModelState.AddModelError(string.Empty, message);
                    await PopulateDropdownsAsync(model, userContext);
                    return View(model);
                }

                TempData["SuccessMessage"] = message;
                return RedirectToAction(nameof(Details), new { id = model.OpportunityId });
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
                var (success, message) = await _opportunityService.DeleteOrArchiveOpportunityAsync(id, userContext);
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
