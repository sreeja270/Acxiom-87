using System.Text;
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
    public class ReportsController : Controller
    {
        private readonly IReportService _reportService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReportsController(
            IReportService reportService,
            UserManager<ApplicationUser> userManager)
        {
            _reportService = reportService;
            _userManager = userManager;
        }

        private UserContext GetUserContext()
        {
            return UserContext.FromClaims(User, HttpContext.Connection.RemoteIpAddress?.ToString());
        }

        private async Task PopulateUsersDropdownAsync(ReportFilterBase model)
        {
            var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
            model.UsersList = new SelectList(users, nameof(ApplicationUser.Id), nameof(ApplicationUser.FullName), model.AssignedToId);
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> CustomerReport(string? status, string? ownerId, DateTime? start, DateTime? end)
        {
            var userContext = GetUserContext();
            var vm = await _reportService.GetCustomerReportAsync(userContext, status, ownerId, start, end);
            await PopulateUsersDropdownAsync(vm);
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> LeadReport(string? status, string? source, string? ownerId, DateTime? start, DateTime? end)
        {
            var userContext = GetUserContext();
            var vm = await _reportService.GetLeadReportAsync(userContext, status, source, ownerId, start, end);
            await PopulateUsersDropdownAsync(vm);
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> FollowUpReport(string? status, string? ownerId, DateTime? start, DateTime? end)
        {
            var userContext = GetUserContext();
            var vm = await _reportService.GetFollowUpReportAsync(userContext, status, ownerId, start, end);
            await PopulateUsersDropdownAsync(vm);
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> OpportunityReport(string? stage, string? status, string? ownerId, DateTime? start, DateTime? end)
        {
            var userContext = GetUserContext();
            var vm = await _reportService.GetOpportunityReportAsync(userContext, stage, status, ownerId, start, end);
            await PopulateUsersDropdownAsync(vm);
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> PipelineReport(string? ownerId)
        {
            var userContext = GetUserContext();
            var vm = await _reportService.GetPipelineReportAsync(userContext, ownerId);
            await PopulateUsersDropdownAsync(vm);
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> ConversionReport(string? ownerId, DateTime? start, DateTime? end)
        {
            var userContext = GetUserContext();
            var vm = await _reportService.GetConversionReportAsync(userContext, ownerId, start, end);
            await PopulateUsersDropdownAsync(vm);
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> UserActivityReport(DateTime? start, DateTime? end)
        {
            var userContext = GetUserContext();
            var vm = await _reportService.GetUserActivityReportAsync(userContext, start, end);
            return View(vm);
        }

        [HttpGet]
        [Authorize(Roles = DbInitializer.AdminRole)]
        public async Task<IActionResult> AuditReport(string? action, string? entity, string? ownerId, DateTime? start, DateTime? end)
        {
            var userContext = GetUserContext();
            var vm = await _reportService.GetAuditReportAsync(userContext, action, entity, ownerId, start, end);
            await PopulateUsersDropdownAsync(vm);
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> ExportPipelineCsv(string? ownerId)
        {
            var userContext = GetUserContext();
            var vm = await _reportService.GetPipelineReportAsync(userContext, ownerId);

            var sb = new StringBuilder();
            sb.AppendLine("Pipeline Stage,Deal Count,Total Pipeline Amount,Weighted Pipeline Amount");
            foreach (var item in vm.StageBreakdown)
            {
                sb.AppendLine($"\"{item.Stage}\",{item.Count},{item.TotalAmount:F2},{item.WeightedAmount:F2}");
            }
            sb.AppendLine($"\"Grand Total\",,{vm.GrandTotalPipeline:F2},{vm.GrandTotalWeighted:F2}");

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", $"PipelineReport_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
        }

        [HttpGet]
        public async Task<IActionResult> ExportCustomerReportCsv(string? status, string? ownerId, DateTime? start, DateTime? end)
        {
            var userContext = GetUserContext();
            var vm = await _reportService.GetCustomerReportAsync(userContext, status, ownerId, start, end);

            var sb = new StringBuilder();
            sb.AppendLine("Customer Code,Customer Name,Company,Email,Phone,Status,Owner,Created Date,Total Opportunities,Opportunity Value");
            foreach (var item in vm.Items)
            {
                sb.AppendLine($"\"{item.CustomerCode}\",\"{item.CustomerName}\",\"{item.CompanyName}\",\"{item.Email}\",\"{item.Phone}\",\"{item.Status}\",\"{item.OwnerName}\",\"{item.CreatedDate:yyyy-MM-dd}\",{item.OpportunityCount},{item.TotalOpportunityValue:F2}");
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", $"CustomerReport_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
        }
    }
}
