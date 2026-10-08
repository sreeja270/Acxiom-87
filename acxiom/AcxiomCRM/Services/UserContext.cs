using System.Security.Claims;
using AcxiomCRM.Data;

namespace AcxiomCRM.Services
{
    public class UserContext
    {
        public string UserId { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? IpAddress { get; set; }

        public bool IsAdmin => Role == DbInitializer.AdminRole;
        public bool IsManager => Role == DbInitializer.ManagerRole;
        public bool IsSalesExecutive => Role == DbInitializer.SalesExecutiveRole;

        public static UserContext FromClaims(ClaimsPrincipal user, string? ipAddress = null)
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var email = user.FindFirstValue(ClaimTypes.Email) ?? user.Identity?.Name ?? string.Empty;
            var name = user.FindFirstValue("FullName") ?? email;

            var role = DbInitializer.SalesExecutiveRole;
            if (user.IsInRole(DbInitializer.AdminRole))
            {
                role = DbInitializer.AdminRole;
            }
            else if (user.IsInRole(DbInitializer.ManagerRole))
            {
                role = DbInitializer.ManagerRole;
            }
            else if (user.IsInRole(DbInitializer.SalesExecutiveRole))
            {
                role = DbInitializer.SalesExecutiveRole;
            }

            return new UserContext
            {
                UserId = userId,
                UserEmail = email,
                UserName = name,
                Role = role,
                IpAddress = ipAddress
            };
        }
    }
}
