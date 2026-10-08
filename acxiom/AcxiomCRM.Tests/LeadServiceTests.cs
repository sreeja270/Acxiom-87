using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AcxiomCRM.Tests
{
    public class LeadServiceTests
    {
        [Fact]
        public async Task CreateLead_WithNegativeExpectedValue_Fails()
        {
            using var context = TestHelpers.CreateInMemoryDbContext(nameof(CreateLead_WithNegativeExpectedValue_Fails));
            var auditMock = TestHelpers.CreateMockAuditService();
            var custService = new CustomerService(context, auditMock.Object, NullLogger<CustomerService>.Instance);
            var service = new LeadService(context, auditMock.Object, custService, NullLogger<LeadService>.Instance);
            var adminContext = TestHelpers.CreateAdminContext();

            var lead = new Lead
            {
                LeadName = "Test Lead",
                Email = "test@lead.com",
                Phone = "+1-555-1111",
                ExpectedValue = -500
            };

            var (success, message, created) = await service.CreateLeadAsync(lead, adminContext);

            Assert.False(success);
            Assert.Equal("Expected value must be numeric and greater than or equal to 0.", message);
            Assert.Null(created);
        }

        [Fact]
        public async Task ConvertLead_CreatesCustomerAndOpportunity_AndUpdatesStatus()
        {
            using var context = TestHelpers.CreateInMemoryDbContext(nameof(ConvertLead_CreatesCustomerAndOpportunity_AndUpdatesStatus));
            var auditMock = TestHelpers.CreateMockAuditService();
            var custService = new CustomerService(context, auditMock.Object, NullLogger<CustomerService>.Instance);
            var service = new LeadService(context, auditMock.Object, custService, NullLogger<LeadService>.Instance);
            var adminContext = TestHelpers.CreateAdminContext();

            var lead = new Lead
            {
                LeadName = "Conversion Candidate",
                CompanyName = "Innovative AI Corp",
                Email = "candidate@innovative.ai",
                Phone = "+1-555-8888",
                Source = "Website",
                Status = "Qualified",
                ExpectedValue = 75000m
            };
            var (_, _, createdLead) = await service.CreateLeadAsync(lead, adminContext);

            var convertModel = new ConvertLeadViewModel
            {
                LeadId = createdLead!.LeadId,
                CreateOpportunity = true,
                OpportunityName = "Enterprise AI Integration Deal",
                OpportunityAmount = 75000m,
                Probability = 50,
                ExpectedCloseDate = DateTime.Today.AddDays(45)
            };

            var (success, message, customer, opp) = await service.ConvertLeadAsync(createdLead.LeadId, convertModel, adminContext);

            Assert.True(success);
            Assert.NotNull(customer);
            Assert.Equal("Conversion Candidate", customer.CustomerName);
            Assert.Equal("Innovative AI Corp", customer.CompanyName);
            Assert.NotNull(opp);
            Assert.Equal(75000m, opp.Amount);
            Assert.Equal(customer.CustomerId, opp.CustomerId);

            var fetchedLead = await service.GetLeadByIdAsync(createdLead.LeadId, adminContext);
            Assert.NotNull(fetchedLead);
            Assert.Equal("Converted", fetchedLead.Status);
            Assert.NotNull(fetchedLead.ConvertedDate);

            // Verify audit logging
            auditMock.Verify(a => a.LogAsync(
                adminContext.UserId,
                adminContext.UserEmail,
                "Lead Conversion",
                "Lead",
                createdLead.LeadId.ToString(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()), Times.Once);
        }
    }
}
