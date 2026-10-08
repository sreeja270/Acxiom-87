using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditService _auditService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            IAuditService auditService,
            ILogger<AccountController> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _auditService = auditService;
            _logger = logger;
        }

        private string? GetClientIpAddress()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                await _auditService.LogAsync(
                    null,
                    model.Email,
                    "Failed Login",
                    "Auth",
                    null,
                    null,
                    null,
                    $"Failed login attempt for non-existent user '{model.Email}'",
                    GetClientIpAddress());

                ModelState.AddModelError(string.Empty, "Invalid login credentials.");
                return View(model);
            }

            // Check if deactivated
            if (!user.IsActive)
            {
                await _auditService.LogAsync(
                    user.Id,
                    user.Email,
                    "Failed Login",
                    "Auth",
                    user.Id,
                    null,
                    null,
                    $"Blocked login attempt for deactivated user '{user.Email}'",
                    GetClientIpAddress());

                ModelState.AddModelError(string.Empty, "Your account has been deactivated. Please contact your system administrator.");
                return View(model);
            }

            // Attempt login with lockout enabled
            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                await _auditService.LogAsync(
                    user.Id,
                    user.Email,
                    "Login",
                    "Auth",
                    user.Id,
                    null,
                    null,
                    $"User '{user.Email}' logged in successfully.",
                    GetClientIpAddress());

                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }
                return RedirectToAction("Index", "Dashboard");
            }

            if (result.IsLockedOut)
            {
                await _auditService.LogAsync(
                    user.Id,
                    user.Email,
                    "Lockout",
                    "Auth",
                    user.Id,
                    null,
                    null,
                    $"User account '{user.Email}' locked out due to excessive failed attempts.",
                    GetClientIpAddress());

                ModelState.AddModelError(string.Empty, "Account locked out due to repeated failed attempts. Please try again after 15 minutes or contact your administrator.");
                return View(model);
            }

            await _auditService.LogAsync(
                user.Id,
                user.Email,
                "Failed Login",
                "Auth",
                user.Id,
                null,
                null,
                $"Failed password authentication attempt for user '{user.Email}'.",
                GetClientIpAddress());

            ModelState.AddModelError(string.Empty, "Invalid login credentials.");
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Dashboard");
            }
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existing = await _userManager.FindByEmailAsync(model.Email);
            if (existing != null)
            {
                ModelState.AddModelError(nameof(model.Email), "A user with this email address already exists.");
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                Department = model.Department ?? "Sales",
                EmailConfirmed = true,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                // Assign role (default to Sales Executive, unless Admin selected)
                var roleToAssign = model.Role == DbInitializer.AdminRole ? DbInitializer.AdminRole : DbInitializer.SalesExecutiveRole;
                await _userManager.AddToRoleAsync(user, roleToAssign);

                await _auditService.LogAsync(
                    user.Id,
                    user.Email,
                    "Create",
                    "User",
                    user.Id,
                    null,
                    $"Role: {roleToAssign}",
                    $"User registered: {user.FullName} ({user.Email})",
                    GetClientIpAddress());

                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Dashboard");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                await _auditService.LogAsync(
                    user.Id,
                    user.Email,
                    "Logout",
                    "Auth",
                    user.Id,
                    null,
                    null,
                    $"User '{user.Email}' logged out.",
                    GetClientIpAddress());
            }

            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var vm = new UserProfileViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                Department = user.Department,
                Role = roles.FirstOrDefault() ?? "Sales Executive",
                IsActive = user.IsActive,
                CreatedDate = user.CreatedDate
            };

            return View(vm);
        }

        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);

                await _auditService.LogAsync(
                    user.Id,
                    user.Email,
                    "Update",
                    "User",
                    user.Id,
                    null,
                    null,
                    $"User '{user.Email}' changed their account password.",
                    GetClientIpAddress());

                TempData["SuccessMessage"] = "Your password has been changed successfully.";
                return RedirectToAction(nameof(Profile));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
