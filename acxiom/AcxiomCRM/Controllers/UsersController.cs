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
    public class UsersController : Controller
    {
        private readonly IUserService _userService;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsersController(IUserService userService, RoleManager<IdentityRole> roleManager)
        {
            _userService = userService;
            _roleManager = roleManager;
        }

        private UserContext GetAdminContext()
        {
            return UserContext.FromClaims(User, HttpContext.Connection.RemoteIpAddress?.ToString());
        }

        private async Task PopulateRolesDropdownAsync(dynamic model)
        {
            var roles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
            model.RolesList = new SelectList(roles, nameof(IdentityRole.Name), nameof(IdentityRole.Name), model.Role);
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? searchTerm, string? roleFilter)
        {
            var users = await _userService.GetUsersAsync(searchTerm, roleFilter);
            var roles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();

            var vm = new UserListViewModel
            {
                Users = users,
                SearchTerm = searchTerm,
                RoleFilter = roleFilter,
                RolesList = new SelectList(roles, nameof(IdentityRole.Name), nameof(IdentityRole.Name), roleFilter)
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new UserCreateViewModel();
            await PopulateRolesDropdownAsync(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateRolesDropdownAsync(model);
                return View(model);
            }

            var adminContext = GetAdminContext();
            var (succeeded, message, _) = await _userService.CreateUserAsync(model, adminContext);
            if (!succeeded)
            {
                ModelState.AddModelError(string.Empty, message);
                await PopulateRolesDropdownAsync(model);
                return View(model);
            }

            TempData["SuccessMessage"] = "User account created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var userManager = HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
            var roles = await userManager.GetRolesAsync(user);

            var model = new UserEditViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Department = user.Department,
                Role = roles.FirstOrDefault() ?? DbInitializer.SalesExecutiveRole,
                IsActive = user.IsActive
            };

            await PopulateRolesDropdownAsync(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateRolesDropdownAsync(model);
                return View(model);
            }

            var adminContext = GetAdminContext();
            var (succeeded, message) = await _userService.UpdateUserAsync(model, adminContext);
            if (!succeeded)
            {
                ModelState.AddModelError(string.Empty, message);
                await PopulateRolesDropdownAsync(model);
                return View(model);
            }

            TempData["SuccessMessage"] = "User profile updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var model = new UserResetPasswordViewModel
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(UserResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var adminContext = GetAdminContext();
            var (succeeded, message) = await _userService.ResetPasswordAsync(model.UserId, model.NewPassword, adminContext);
            if (!succeeded)
            {
                ModelState.AddModelError(string.Empty, message);
                return View(model);
            }

            TempData["SuccessMessage"] = "Password has been successfully reset.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unlock(string id)
        {
            var adminContext = GetAdminContext();
            var (succeeded, message) = await _userService.UnlockAccountAsync(id, adminContext);
            if (succeeded)
            {
                TempData["SuccessMessage"] = message;
            }
            else
            {
                TempData["ErrorMessage"] = message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var adminContext = GetAdminContext();
            var (succeeded, message) = await _userService.ToggleUserActiveStatusAsync(id, adminContext);
            if (succeeded)
            {
                TempData["SuccessMessage"] = message;
            }
            else
            {
                TempData["ErrorMessage"] = message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
