using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AcxiomCRM.Tests
{
    public class FollowUpServiceTests
    {
        [Fact]
        public async Task CreateFollowUp_WithPastDate_Fails()
        {
            using var context = TestHelpers.CreateInMemoryDbContext(nameof(CreateFollowUp_WithPastDate_Fails));
            var auditMock = TestHelpers.CreateMockAuditService();
            var service = new FollowUpService(context, auditMock.Object, NullLogger<FollowUpService>.Instance);
            var adminContext = TestHelpers.CreateAdminContext();

            var followUp = new FollowUp
            {
                FollowUpDate = DateTime.Today.AddDays(-1), // In the past!
                FollowUpType = "Call",
                Remarks = "Past follow-up call",
                Status = "Planned",
                AssignedToId = adminContext.UserId
            };

            var (success, message, created) = await service.CreateFollowUpAsync(followUp, adminContext);

            Assert.False(success);
            Assert.Equal("Follow-up date cannot be earlier than today.", message);
            Assert.Null(created);
        }

        [Fact]
        public async Task CreateFollowUp_WithFutureDate_Succeeds()
        {
            using var context = TestHelpers.CreateInMemoryDbContext(nameof(CreateFollowUp_WithFutureDate_Succeeds));
            var auditMock = TestHelpers.CreateMockAuditService();
            var service = new FollowUpService(context, auditMock.Object, NullLogger<FollowUpService>.Instance);
            var adminContext = TestHelpers.CreateAdminContext();

            var followUp = new FollowUp
            {
                FollowUpDate = DateTime.Today.AddDays(2),
                FollowUpType = "Meeting",
                Remarks = "Future strategic review meeting",
                Status = "Planned",
                AssignedToId = adminContext.UserId
            };

            var (success, message, created) = await service.CreateFollowUpAsync(followUp, adminContext);

            Assert.True(success);
            Assert.NotNull(created);
            Assert.Equal("Planned", created.Status);
        }

        [Fact]
        public async Task UpdateStatus_ToCompleted_SetsStatusAndDate()
        {
            using var context = TestHelpers.CreateInMemoryDbContext(nameof(UpdateStatus_ToCompleted_SetsStatusAndDate));
            var auditMock = TestHelpers.CreateMockAuditService();
            var service = new FollowUpService(context, auditMock.Object, NullLogger<FollowUpService>.Instance);
            var adminContext = TestHelpers.CreateAdminContext();

            var followUp = new FollowUp
            {
                FollowUpDate = DateTime.Today.AddDays(1),
                FollowUpType = "Email",
                Remarks = "Send contract draft",
                Status = "Planned",
                AssignedToId = adminContext.UserId
            };
            var (_, _, created) = await service.CreateFollowUpAsync(followUp, adminContext);

            var (success, message) = await service.UpdateStatusAsync(created!.FollowUpId, "Completed", "Contract signed", null, adminContext);

            Assert.True(success);
            var updated = await service.GetFollowUpByIdAsync(created.FollowUpId, adminContext);
            Assert.NotNull(updated);
            Assert.Equal("Completed", updated.Status);
            Assert.NotNull(updated.CompletedDate);
            Assert.Equal("Contract signed", updated.CompletionNotes);
        }
    }
}
