using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AcxiomCRM.Tests
{
    public class OpportunityServiceTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(-500)]
        public async Task CreateOpportunity_WithZeroOrNegativeAmount_Fails(decimal amount)
        {
            using var context = TestHelpers.CreateInMemoryDbContext(nameof(CreateOpportunity_WithZeroOrNegativeAmount_Fails) + amount);
            var auditMock = TestHelpers.CreateMockAuditService();
            var service = new OpportunityService(context, auditMock.Object, NullLogger<OpportunityService>.Instance);
            var adminContext = TestHelpers.CreateAdminContext();

            var opp = new Opportunity
            {
                OpportunityName = "Zero Amount Deal",
                CustomerId = 1,
                Amount = amount,
                Stage = "Qualification",
                Probability = 20,
                ExpectedCloseDate = DateTime.Today.AddDays(30)
            };

            var (success, message, created) = await service.CreateOpportunityAsync(opp, adminContext);

            Assert.False(success);
            Assert.Equal("Opportunity Amount must be greater than 0.", message);
            Assert.Null(created);
        }

        [Theory]
        [InlineData(-5)]
        [InlineData(105)]
        [InlineData(200)]
        public async Task CreateOpportunity_WithInvalidProbability_Fails(int probability)
        {
            using var context = TestHelpers.CreateInMemoryDbContext(nameof(CreateOpportunity_WithInvalidProbability_Fails) + probability);
            var auditMock = TestHelpers.CreateMockAuditService();
            var service = new OpportunityService(context, auditMock.Object, NullLogger<OpportunityService>.Instance);
            var adminContext = TestHelpers.CreateAdminContext();

            var opp = new Opportunity
            {
                OpportunityName = "Invalid Prob Deal",
                CustomerId = 1,
                Amount = 10000,
                Stage = "Qualification",
                Probability = probability,
                ExpectedCloseDate = DateTime.Today.AddDays(30)
            };

            var (success, message, created) = await service.CreateOpportunityAsync(opp, adminContext);

            Assert.False(success);
            Assert.Equal("Probability must be between 0 and 100.", message);
            Assert.Null(created);
        }

        [Fact]
        public async Task CreateOpportunity_WithPastExpectedCloseDate_Fails()
        {
            using var context = TestHelpers.CreateInMemoryDbContext(nameof(CreateOpportunity_WithPastExpectedCloseDate_Fails));
            var auditMock = TestHelpers.CreateMockAuditService();
            var service = new OpportunityService(context, auditMock.Object, NullLogger<OpportunityService>.Instance);
            var adminContext = TestHelpers.CreateAdminContext();

            var opp = new Opportunity
            {
                OpportunityName = "Past Date Deal",
                CustomerId = 1,
                Amount = 10000,
                Stage = "Qualification",
                Probability = 50,
                ExpectedCloseDate = DateTime.Today.AddDays(-2), // In the past!
                Status = "Open"
            };

            var (success, message, created) = await service.CreateOpportunityAsync(opp, adminContext);

            Assert.False(success);
            Assert.Equal("Expected Close Date cannot be in the past.", message);
            Assert.Null(created);
        }

        [Fact]
        public void Opportunity_WeightedPipeline_IsComputedCorrectly()
        {
            var opp = new Opportunity
            {
                Amount = 100000m,
                Probability = 60
            };

            // Weighted pipeline = 100,000 * 60 / 100 = 60,000
            Assert.Equal(60000.00m, opp.WeightedPipeline);
        }

        [Fact]
        public async Task SalesExecutive_CannotAccess_OtherOpportunity()
        {
            using var context = TestHelpers.CreateInMemoryDbContext(nameof(SalesExecutive_CannotAccess_OtherOpportunity));
            var auditMock = TestHelpers.CreateMockAuditService();
            var service = new OpportunityService(context, auditMock.Object, NullLogger<OpportunityService>.Instance);

            var salesUser1 = TestHelpers.CreateSalesExecutiveContext("sales-1", "sales1@acxiomcrm.com");
            var salesUser2 = TestHelpers.CreateSalesExecutiveContext("sales-2", "sales2@acxiomcrm.com");

            var opp = new Opportunity
            {
                OpportunityName = "Sales 1 Exclusive Deal",
                CustomerId = 1,
                Amount = 50000,
                Stage = "Proposal",
                Probability = 70,
                ExpectedCloseDate = DateTime.Today.AddDays(15),
                AssignedToId = "sales-1"
            };
            var (_, _, created) = await service.CreateOpportunityAsync(opp, salesUser1);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await service.GetOpportunityByIdAsync(created!.OpportunityId, salesUser2);
            });
        }
    }
}
