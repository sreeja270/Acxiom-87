using AcxiomCRM.Data;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers
{
    [Authorize(Roles = DbInitializer.AdminRole)]
    public class RolesController : Controller
    {
        private readonly IUserService _userService;

        public RolesController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var roles = await _userService.GetRolesSummaryAsync();
            var vm = new RoleListViewModel { Roles = roles };
            return View(vm);
        }
    }
}
