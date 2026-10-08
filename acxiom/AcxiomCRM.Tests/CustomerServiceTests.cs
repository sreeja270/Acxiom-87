using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AcxiomCRM.Tests
{
    public class CustomerServiceTests
    {
        [Fact]
        public async Task CreateCustomer_WithEmptyName_Fails()
        {
            using var context = TestHelpers.CreateInMemoryDbContext(nameof(CreateCustomer_WithEmptyName_Fails));
            var auditMock = TestHelpers.CreateMockAuditService();
            var service = new CustomerService(context, auditMock.Object, NullLogger<CustomerService>.Instance);
            var adminContext = TestHelpers.CreateAdminContext();

            var customer = new Customer
            {
                CustomerName = "",
                Email = "test@company.com",
                Phone = "+1-555-1234"
            };

            var (success, message, created) = await service.CreateCustomerAsync(customer, adminContext);

            Assert.False(success);
            Assert.Equal("Customer Name is required.", message);
            Assert.Null(created);
        }

        [Fact]
        public async Task CreateCustomer_WithDuplicateEmail_Fails()
        {
            using var context = TestHelpers.CreateInMemoryDbContext(nameof(CreateCustomer_WithDuplicateEmail_Fails));
            var auditMock = TestHelpers.CreateMockAuditService();
            var service = new CustomerService(context, auditMock.Object, NullLogger<CustomerService>.Instance);
            var adminContext = TestHelpers.CreateAdminContext();

            // First customer
            var cust1 = new Customer
            {
                CustomerCode = "CUST-001",
                CustomerName = "First Customer",
                Email = "duplicate@company.com",
                Phone = "+1-555-1111"
            };
            await service.CreateCustomerAsync(cust1, adminContext);

            // Second customer with same email
            var cust2 = new Customer
            {
                CustomerName = "Second Customer",
                Email = "duplicate@company.com",
                Phone = "+1-555-2222"
            };
            var (success, message, created) = await service.CreateCustomerAsync(cust2, adminContext);

            Assert.False(success);
            Assert.Equal("Email already exists.", message);
            Assert.Null(created);
        }

        [Fact]
        public async Task CreateCustomer_WithDuplicatePhone_Fails()
        {
            using var context = TestHelpers.CreateInMemoryDbContext(nameof(CreateCustomer_WithDuplicatePhone_Fails));
            var auditMock = TestHelpers.CreateMockAuditService();
            var service = new CustomerService(context, auditMock.Object, NullLogger<CustomerService>.Instance);
            var adminContext = TestHelpers.CreateAdminContext();

            var cust1 = new Customer
            {
                CustomerCode = "CUST-001",
                CustomerName = "First Customer",
                Email = "first@company.com",
                Phone = "+1-555-9999"
            };
            await service.CreateCustomerAsync(cust1, adminContext);

            var cust2 = new Customer
            {
                CustomerName = "Second Customer",
                Email = "second@company.com",
                Phone = "+1-555-9999"
            };
            var (success, message, created) = await service.CreateCustomerAsync(cust2, adminContext);

            Assert.False(success);
            Assert.Equal("Phone number already exists.", message);
            Assert.Null(created);
        }

        [Fact]
        public async Task CreateCustomer_DuplicateNameAndCompany_Fails()
        {
            using var context = TestHelpers.CreateInMemoryDbContext(nameof(CreateCustomer_DuplicateNameAndCompany_Fails));
            var auditMock = TestHelpers.CreateMockAuditService();
            var service = new CustomerService(context, auditMock.Object, NullLogger<CustomerService>.Instance);
            var adminContext = TestHelpers.CreateAdminContext();

            var cust1 = new Customer
            {
                CustomerCode = "CUST-001",
                CustomerName = "Alice Smith",
                CompanyName = "Acme Corp",
                Email = "alice@acme.com",
                Phone = "+1-555-3333"
            };
            await service.CreateCustomerAsync(cust1, adminContext);

            var cust2 = new Customer
            {
                CustomerName = "Alice Smith",
                CompanyName = "Acme Corp",
                Email = "asmith@acme.com",
                Phone = "+1-555-4444"
            };
            var (success, message, created) = await service.CreateCustomerAsync(cust2, adminContext);

            Assert.False(success);
            Assert.Equal("Duplicate customer cannot be created.", message);
            Assert.Null(created);
        }

        [Fact]
        public async Task SalesExecutive_CannotAccess_OtherSalesExecutiveCustomer()
        {
            using var context = TestHelpers.CreateInMemoryDbContext(nameof(SalesExecutive_CannotAccess_OtherSalesExecutiveCustomer));
            var auditMock = TestHelpers.CreateMockAuditService();
            var service = new CustomerService(context, auditMock.Object, NullLogger<CustomerService>.Instance);

            var salesUser1 = TestHelpers.CreateSalesExecutiveContext("sales-1", "sales1@acxiomcrm.com");
            var salesUser2 = TestHelpers.CreateSalesExecutiveContext("sales-2", "sales2@acxiomcrm.com");

            // Customer belonging to sales-1
            var customer = new Customer
            {
                CustomerCode = "CUST-001",
                CustomerName = "Owner Customer",
                Email = "owner@test.com",
                Phone = "+1-555-5555",
                AssignedToId = "sales-1"
            };
            var (_, _, created) = await service.CreateCustomerAsync(customer, salesUser1);

            // sales-2 tries to access sales-1's customer
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await service.GetCustomerByIdAsync(created!.CustomerId, salesUser2);
            });
        }

        [Fact]
        public async Task Admin_CanAccess_AnyCustomer()
        {
            using var context = TestHelpers.CreateInMemoryDbContext(nameof(Admin_CanAccess_AnyCustomer));
            var auditMock = TestHelpers.CreateMockAuditService();
            var service = new CustomerService(context, auditMock.Object, NullLogger<CustomerService>.Instance);

            var salesUser1 = TestHelpers.CreateSalesExecutiveContext("sales-1", "sales1@acxiomcrm.com");
            var adminContext = TestHelpers.CreateAdminContext();

            var customer = new Customer
            {
                CustomerCode = "CUST-001",
                CustomerName = "Target Customer",
                Email = "target@test.com",
                Phone = "+1-555-6666"
            };
            var (_, _, created) = await service.CreateCustomerAsync(customer, salesUser1);

            // Admin accesses it
            var fetched = await service.GetCustomerByIdAsync(created!.CustomerId, adminContext);
            Assert.NotNull(fetched);
            Assert.Equal("Target Customer", fetched.CustomerName);
        }

        [Fact]
        public async Task CreateCustomer_GeneratesAuditRecord()
        {
            using var context = TestHelpers.CreateInMemoryDbContext(nameof(CreateCustomer_GeneratesAuditRecord));
            var auditMock = TestHelpers.CreateMockAuditService();
            var service = new CustomerService(context, auditMock.Object, NullLogger<CustomerService>.Instance);
            var adminContext = TestHelpers.CreateAdminContext();

            var customer = new Customer
            {
                CustomerName = "Audit Test",
                Email = "audit@test.com",
                Phone = "+1-555-7777"
            };

            await service.CreateCustomerAsync(customer, adminContext);

            // Verify audit service was invoked with "Create" and "Customer"
            auditMock.Verify(a => a.LogAsync(
                adminContext.UserId,
                adminContext.UserEmail,
                "Create",
                "Customer",
                It.IsAny<string?>(),
                null,
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()), Times.Once);
        }
    }
}
