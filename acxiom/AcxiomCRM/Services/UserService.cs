using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services
{
    public interface IUserService
    {
        Task<List<UserItemViewModel>> GetUsersAsync(string? searchTerm, string? roleFilter);
        Task<ApplicationUser?> GetUserByIdAsync(string id);
        Task<(bool Succeeded, string Message, ApplicationUser? User)> CreateUserAsync(UserCreateViewModel model, UserContext adminContext);
        Task<(bool Succeeded, string Message)> UpdateUserAsync(UserEditViewModel model, UserContext adminContext);
        Task<(bool Succeeded, string Message)> ResetPasswordAsync(string userId, string newPassword, UserContext adminContext);
        Task<(bool Succeeded, string Message)> UnlockAccountAsync(string userId, UserContext adminContext);
        Task<(bool Succeeded, string Message)> ToggleUserActiveStatusAsync(string userId, UserContext adminContext);
        Task<List<RoleItemViewModel>> GetRolesSummaryAsync();
    }

    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly ILogger<UserService> _logger;

        public UserService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            IAuditService auditService,
            ILogger<UserService> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<List<UserItemViewModel>> GetUsersAsync(string? searchTerm, string? roleFilter)
        {
            var query = _userManager.Users.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(u =>
                    u.FullName.ToLower().Contains(term) ||
                    (u.Email != null && u.Email.ToLower().Contains(term)) ||
                    (u.Department != null && u.Department.ToLower().Contains(term)));
            }

            var users = await query.OrderBy(u => u.FullName).ToListAsync();
            var list = new List<UserItemViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var roleName = roles.FirstOrDefault() ?? "None";

                if (!string.IsNullOrWhiteSpace(roleFilter) && roleName != roleFilter)
                {
                    continue;
                }

                var isLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;

                list.Add(new UserItemViewModel
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email ?? string.Empty,
                    Department = user.Department,
                    Role = roleName,
                    IsActive = user.IsActive,
                    IsLockedOut = isLocked,
                    LockoutEnd = user.LockoutEnd,
                    CreatedDate = user.CreatedDate
                });
            }

            return list;
        }

        public async Task<ApplicationUser?> GetUserByIdAsync(string id)
        {
            return await _userManager.FindByIdAsync(id);
        }

        public async Task<(bool Succeeded, string Message, ApplicationUser? User)> CreateUserAsync(UserCreateViewModel model, UserContext adminContext)
        {
            if (!adminContext.IsAdmin)
            {
                throw new UnauthorizedAccessException("Only Administrators can create users.");
            }

            var existing = await _userManager.FindByEmailAsync(model.Email);
            if (existing != null)
            {
                return (false, "A user with this email address already exists.", null);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                Department = model.Department,
                EmailConfirmed = true,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return (false, errors, null);
            }

            // Assign role
            if (!string.IsNullOrWhiteSpace(model.Role) && await _roleManager.RoleExistsAsync(model.Role))
            {
                await _userManager.AddToRoleAsync(user, model.Role);
            }

            await _auditService.LogAsync(
                adminContext.UserId,
                adminContext.UserEmail,
                "Create",
                "User",
                user.Id,
                null,
                $"Email: {user.Email}, Role: {model.Role}",
                $"Created user {user.FullName} with role {model.Role}",
                adminContext.IpAddress);

            return (true, "User created successfully.", user);
        }

        public async Task<(bool Succeeded, string Message)> UpdateUserAsync(UserEditViewModel model, UserContext adminContext)
        {
            if (!adminContext.IsAdmin)
            {
                throw new UnauthorizedAccessException("Only Administrators can modify user accounts.");
            }

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
            {
                return (false, "User not found.");
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            var currentRole = currentRoles.FirstOrDefault() ?? "None";

            var oldSummary = $"Name: {user.FullName}, Email: {user.Email}, Role: {currentRole}, Active: {user.IsActive}";

            user.FullName = model.FullName;
            user.Department = model.Department;
            user.IsActive = model.IsActive;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return (false, string.Join(", ", updateResult.Errors.Select(e => e.Description)));
            }

            // Update role if changed
            if (currentRole != model.Role && !string.IsNullOrWhiteSpace(model.Role))
            {
                if (currentRoles.Any())
                {
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                }
                await _userManager.AddToRoleAsync(user, model.Role);

                await _auditService.LogAsync(
                    adminContext.UserId,
                    adminContext.UserEmail,
                    "Role Change",
                    "User",
                    user.Id,
                    $"Role: {currentRole}",
                    $"Role: {model.Role}",
                    $"Changed user role from {currentRole} to {model.Role}",
                    adminContext.IpAddress);
            }

            var newSummary = $"Name: {user.FullName}, Email: {user.Email}, Role: {model.Role}, Active: {user.IsActive}";

            await _auditService.LogAsync(
                adminContext.UserId,
                adminContext.UserEmail,
                "Update",
                "User",
                user.Id,
                oldSummary,
                newSummary,
                $"Updated user profile for {user.FullName}",
                adminContext.IpAddress);

            return (true, "User profile updated successfully.");
        }

        public async Task<(bool Succeeded, string Message)> ResetPasswordAsync(string userId, string newPassword, UserContext adminContext)
        {
            if (!adminContext.IsAdmin)
            {
                throw new UnauthorizedAccessException("Only Administrators can reset passwords.");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return (false, "User not found.");
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (!result.Succeeded)
            {
                return (false, string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            await _auditService.LogAsync(
                adminContext.UserId,
                adminContext.UserEmail,
                "Password Reset",
                "User",
                user.Id,
                null,
                null,
                $"Administrator reset password for user {user.Email}",
                adminContext.IpAddress);

            return (true, "Password has been successfully reset.");
        }

        public async Task<(bool Succeeded, string Message)> UnlockAccountAsync(string userId, UserContext adminContext)
        {
            if (!adminContext.IsAdmin)
            {
                throw new UnauthorizedAccessException("Only Administrators can unlock accounts.");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return (false, "User not found.");
            }

            // Reset lockout end date
            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);

            await _auditService.LogAsync(
                adminContext.UserId,
                adminContext.UserEmail,
                "Unlock",
                "User",
                user.Id,
                "Status: Locked",
                "Status: Unlocked",
                $"Administrator unlocked account for user {user.Email}",
                adminContext.IpAddress);

            return (true, "Account has been unlocked.");
        }

        public async Task<(bool Succeeded, string Message)> ToggleUserActiveStatusAsync(string userId, UserContext adminContext)
        {
            if (!adminContext.IsAdmin)
            {
                throw new UnauthorizedAccessException("Only Administrators can activate or deactivate accounts.");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return (false, "User not found.");
            }

            // Prevent admin deactivating self
            if (user.Id == adminContext.UserId)
            {
                return (false, "You cannot deactivate your own administrative account.");
            }

            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);

            await _auditService.LogAsync(
                adminContext.UserId,
                adminContext.UserEmail,
                user.IsActive ? "Activation" : "Deactivation",
                "User",
                user.Id,
                $"Active: {!user.IsActive}",
                $"Active: {user.IsActive}",
                $"User account {user.Email} marked as {(user.IsActive ? "Active" : "Inactive")}",
                adminContext.IpAddress);

            return (true, $"User account is now {(user.IsActive ? "Active" : "Inactive")}.");
        }

        public async Task<List<RoleItemViewModel>> GetRolesSummaryAsync()
        {
            var roles = await _roleManager.Roles.ToListAsync();
            var list = new List<RoleItemViewModel>();

            foreach (var role in roles)
            {
                var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
                var desc = role.Name switch
                {
                    DbInitializer.AdminRole => "Full unrestricted enterprise access to all records, user management, security audit logs, and configuration.",
                    DbInitializer.ManagerRole => "Team-level pipeline monitoring, reports, customer and deal assignment oversight.",
                    DbInitializer.SalesExecutiveRole => "Direct customer relationship management, assigned lead qualification, and opportunity pipeline execution.",
                    _ => "Standard system role"
                };

                var perms = role.Name switch
                {
                    DbInitializer.AdminRole => new List<string> { "Full CRM Access", "Manage Users & Roles", "Audit Logs", "System Reports", "APIs", "Account Unlock" },
                    DbInitializer.ManagerRole => new List<string> { "Team CRM Access", "Pipeline Management", "Team Reports", "Lead Reassignment", "Customer Oversight" },
                    DbInitializer.SalesExecutiveRole => new List<string> { "Assigned Records Access", "Create Customers & Leads", "Manage Follow-Ups", "Opportunity Tracking", "Lead Conversion" },
                    _ => new List<string> { "Standard Access" }
                };

                list.Add(new RoleItemViewModel
                {
                    Id = role.Id,
                    Name = role.Name!,
                    UserCount = usersInRole.Count,
                    Description = desc,
                    Permissions = perms
                });
            }

            return list;
        }
    }
}
