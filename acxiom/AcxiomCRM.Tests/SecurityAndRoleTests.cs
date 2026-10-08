using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AcxiomCRM.Tests
{
    public class SecurityAndRoleTests
    {
        [Fact]
        public async Task SalesExecutive_IsForbidden_FromAccessingAuditLogs()
        {
            using var context = TestHelpers.CreateInMemoryDbContext(nameof(SalesExecutive_IsForbidden_FromAccessingAuditLogs));
            var service = new AuditService(context, NullLogger<AuditService>.Instance);
            var salesContext = TestHelpers.CreateSalesExecutiveContext();

            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await service.GetAuditLogsAsync(salesContext, null, null, null, null, null, null, 1, 10);
            });
        }

        [Fact]
        public async Task NonAdmin_CannotCreateUser()
        {
            using var context = TestHelpers.CreateInMemoryDbContext(nameof(NonAdmin_CannotCreateUser));
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            var userManagerMock = new Mock<UserManager<ApplicationUser>>(userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
            var roleStoreMock = new Mock<IRoleStore<IdentityRole>>();
            var roleManagerMock = new Mock<RoleManager<IdentityRole>>(roleStoreMock.Object, null!, null!, null!, null!);
            var auditMock = TestHelpers.CreateMockAuditService();

            var service = new UserService(userManagerMock.Object, roleManagerMock.Object, context, auditMock.Object, NullLogger<UserService>.Instance);
            var salesContext = TestHelpers.CreateSalesExecutiveContext();

            var model = new UserCreateViewModel
            {
                FullName = "Attacker Account",
                Email = "attacker@hack.com",
                Password = "Password@123",
                ConfirmPassword = "Password@123",
                Role = "Admin"
            };

            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await service.CreateUserAsync(model, salesContext);
            });
        }

        [Fact]
        public async Task NonAdmin_CannotResetUserPassword()
        {
            using var context = TestHelpers.CreateInMemoryDbContext(nameof(NonAdmin_CannotResetUserPassword));
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            var userManagerMock = new Mock<UserManager<ApplicationUser>>(userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
            var roleStoreMock = new Mock<IRoleStore<IdentityRole>>();
            var roleManagerMock = new Mock<RoleManager<IdentityRole>>(roleStoreMock.Object, null!, null!, null!, null!);
            var auditMock = TestHelpers.CreateMockAuditService();

            var service = new UserService(userManagerMock.Object, roleManagerMock.Object, context, auditMock.Object, NullLogger<UserService>.Instance);
            var managerContext = TestHelpers.CreateManagerContext();

            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await service.ResetPasswordAsync("target-user-id", "NewPassword@123", managerContext);
            });
        }

        [Fact]
        public async Task NonAdmin_CannotUnlockAccount()
        {
            using var context = TestHelpers.CreateInMemoryDbContext(nameof(NonAdmin_CannotUnlockAccount));
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            var userManagerMock = new Mock<UserManager<ApplicationUser>>(userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
            var roleStoreMock = new Mock<IRoleStore<IdentityRole>>();
            var roleManagerMock = new Mock<RoleManager<IdentityRole>>(roleStoreMock.Object, null!, null!, null!, null!);
            var auditMock = TestHelpers.CreateMockAuditService();

            var service = new UserService(userManagerMock.Object, roleManagerMock.Object, context, auditMock.Object, NullLogger<UserService>.Instance);
            var salesContext = TestHelpers.CreateSalesExecutiveContext();

            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await service.UnlockAccountAsync("locked-user-id", salesContext);
            });
        }
    }
}
