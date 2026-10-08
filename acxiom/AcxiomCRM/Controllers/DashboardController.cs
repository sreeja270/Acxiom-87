using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string dateFilter = "This Month", DateTime? customStart = null, DateTime? customEnd = null)
        {
            var userContext = UserContext.FromClaims(User, HttpContext.Connection.RemoteIpAddress?.ToString());
            var viewModel = await _dashboardService.GetDashboardDataAsync(userContext, dateFilter, customStart, customEnd);
            return View(viewModel);
        }
    }
}
