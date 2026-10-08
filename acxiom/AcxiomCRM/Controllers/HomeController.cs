using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Dashboard");
            }
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            _logger.LogError("Internal application error occurred. Request ID: {RequestId}", requestId);
            return View("Error", requestId);
        }

        public IActionResult StatusCodeError(int code)
        {
            ViewBag.StatusCode = code;
            ViewBag.StatusMessage = code switch
            {
                404 => "The requested page or resource could not be found.",
                403 => "You are not authorized to view or access this protected resource.",
                401 => "Authentication is required to access this CRM resource.",
                500 => "An unexpected server error occurred. Our engineering team has been notified.",
                _ => "An unexpected HTTP status code was encountered."
            };
            return View("StatusCodeError");
        }
    }
}
