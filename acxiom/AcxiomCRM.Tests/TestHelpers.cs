using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace AcxiomCRM.Tests
{
    public static class TestHelpers
    {
        public static ApplicationDbContext CreateInMemoryDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            var context = new ApplicationDbContext(options);
            context.Database.EnsureCreated();

            // Seed essential test users and customer for EF navigation resolving
            if (!context.Users.Any())
            {
                var users = new List<ApplicationUser>
                {
                    new() { Id = "admin-1", UserName = "admin@acxiomcrm.com", Email = "admin@acxiomcrm.com", FullName = "System Admin" },
                    new() { Id = "manager-1", UserName = "manager@acxiomcrm.com", Email = "manager@acxiomcrm.com", FullName = "Sales Manager" },
                    new() { Id = "sales-1", UserName = "sales1@acxiomcrm.com", Email = "sales1@acxiomcrm.com", FullName = "David Miller" },
                    new() { Id = "sales-2", UserName = "sales2@acxiomcrm.com", Email = "sales2@acxiomcrm.com", FullName = "Emma Watson" }
                };
                context.Users.AddRange(users);

                var customer = new Customer
                {
                    CustomerId = 1,
                    CustomerCode = "CUST-1001",
                    CustomerName = "Apex Global Logistics",
                    Email = "apex@global.com",
                    Phone = "+1-555-0101",
                    Status = "Active",
                    AssignedToId = "sales-1"
                };
                context.Customers.Add(customer);
                context.SaveChanges();
            }

            return context;
        }

        public static Mock<IAuditService> CreateMockAuditService()
        {
            var mock = new Mock<IAuditService>();
            mock.Setup(a => a.LogAsync(
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()))
                .Returns(Task.CompletedTask);
            return mock;
        }

        public static UserContext CreateAdminContext()
        {
            return new UserContext
            {
                UserId = "admin-1",
                UserEmail = "admin@acxiomcrm.com",
                UserName = "System Admin",
                Role = DbInitializer.AdminRole
            };
        }

        public static UserContext CreateManagerContext()
        {
            return new UserContext
            {
                UserId = "manager-1",
                UserEmail = "manager@acxiomcrm.com",
                UserName = "Sales Manager",
                Role = DbInitializer.ManagerRole
            };
        }

        public static UserContext CreateSalesExecutiveContext(string userId = "sales-1", string email = "sales1@acxiomcrm.com")
        {
            return new UserContext
            {
                UserId = userId,
                UserEmail = email,
                UserName = "Sales Exec",
                Role = DbInitializer.SalesExecutiveRole
            };
        }
    }
}
