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
    [Authorize(Roles = DbInitializer.AdminRole)]
    public class AuditLogsController : Controller
    {
        private readonly IAuditService _auditService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public AuditLogsController(
            IAuditService auditService,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _auditService = auditService;
            _userManager = userManager;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? searchTerm,
            string? actionFilter,
            string? entityFilter,
            string? userIdFilter,
            DateTime? startDate,
            DateTime? endDate,
            int pageIndex = 1)
        {
            var userContext = UserContext.FromClaims(User, HttpContext.Connection.RemoteIpAddress?.ToString());
            var pageSize = 20;

            var logs = await _auditService.GetAuditLogsAsync(
                userContext,
                searchTerm,
                actionFilter,
                entityFilter,
                userIdFilter,
                startDate,
                endDate,
                pageIndex,
                pageSize);

            var users = await _userManager.Users.OrderBy(u => u.FullName).ToListAsync();
            var actions = await _context.AuditLogs.Select(a => a.Action).Distinct().ToListAsync();
            var entities = await _context.AuditLogs.Select(a => a.EntityName).Distinct().ToListAsync();

            var vm = new AuditLogListViewModel
            {
                AuditLogs = logs,
                SearchTerm = searchTerm,
                ActionFilter = actionFilter,
                EntityFilter = entityFilter,
                UserIdFilter = userIdFilter,
                StartDate = startDate,
                EndDate = endDate,
                PageIndex = pageIndex,
                UsersList = new SelectList(users, nameof(ApplicationUser.Id), nameof(ApplicationUser.FullName), userIdFilter),
                ActionsList = new SelectList(actions, actionFilter),
                EntitiesList = new SelectList(entities, entityFilter)
            };

            return View(vm);
        }
    }
}
