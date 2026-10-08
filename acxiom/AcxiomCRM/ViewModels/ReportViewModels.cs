using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.ViewModels
{
    public class ReportFilterBase
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? AssignedToId { get; set; }
        public SelectList? UsersList { get; set; }
    }

    public class CustomerReportViewModel : ReportFilterBase
    {
        public string? StatusFilter { get; set; }
        public List<CustomerReportItem> Items { get; set; } = new();
        public int TotalActive { get; set; }
        public int TotalInactive { get; set; }
    }

    public class CustomerReportItem
    {
        public string CustomerCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? OwnerName { get; set; }
        public DateTime CreatedDate { get; set; }
        public int OpportunityCount { get; set; }
        public decimal TotalOpportunityValue { get; set; }
    }

    public class LeadReportViewModel : ReportFilterBase
    {
        public string? StatusFilter { get; set; }
        public string? SourceFilter { get; set; }
        public List<LeadReportItem> Items { get; set; } = new();
        public int TotalLeads { get; set; }
        public int ConvertedCount { get; set; }
        public decimal TotalExpectedValue { get; set; }
    }

    public class LeadReportItem
    {
        public string LeadCode { get; set; } = string.Empty;
        public string LeadName { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string Source { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal ExpectedValue { get; set; }
        public string? OwnerName { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsConverted { get; set; }
        public DateTime? ConvertedDate { get; set; }
    }

    public class FollowUpReportViewModel : ReportFilterBase
    {
        public string? StatusFilter { get; set; }
        public List<FollowUpReportItem> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int OverdueCount { get; set; }
        public int CompletedCount { get; set; }
        public int PlannedCount { get; set; }
    }

    public class FollowUpReportItem
    {
        public int FollowUpId { get; set; }
        public DateTime FollowUpDate { get; set; }
        public string FollowUpType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? CustomerOrLeadName { get; set; }
        public string? Remarks { get; set; }
        public string? OwnerName { get; set; }
        public bool IsOverdue { get; set; }
    }

    public class OpportunityReportViewModel : ReportFilterBase
    {
        public string? StageFilter { get; set; }
        public string? StatusFilter { get; set; }
        public List<OpportunityReportItem> Items { get; set; } = new();
        public decimal TotalAmount { get; set; }
        public decimal TotalWeightedAmount { get; set; }
        public int WonCount { get; set; }
        public int LostCount { get; set; }
    }

    public class OpportunityReportItem
    {
        public string OpportunityCode { get; set; } = string.Empty;
        public string OpportunityName { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Stage { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int Probability { get; set; }
        public decimal WeightedAmount { get; set; }
        public DateTime ExpectedCloseDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? OwnerName { get; set; }
    }

    public class PipelineReportViewModel : ReportFilterBase
    {
        public decimal GrandTotalPipeline { get; set; }
        public decimal GrandTotalWeighted { get; set; }
        public List<StagePipelineSummary> StageBreakdown { get; set; } = new();
        public List<OwnerPipelineSummary> OwnerBreakdown { get; set; } = new();
    }

    public class StagePipelineSummary
    {
        public string Stage { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal WeightedAmount { get; set; }
    }

    public class OwnerPipelineSummary
    {
        public string OwnerName { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal WeightedAmount { get; set; }
    }

    public class ConversionReportViewModel : ReportFilterBase
    {
        public int TotalLeads { get; set; }
        public int ConvertedLeads { get; set; }
        public int UnconvertedLeads { get; set; }
        public double ConversionRatePercentage { get; set; }
        public decimal TotalWonRevenue { get; set; }
        public decimal TotalLostRevenue { get; set; }
        public double OpportunityWinRatePercentage { get; set; }
        public List<SourceConversionSummary> BySource { get; set; } = new();
    }

    public class SourceConversionSummary
    {
        public string Source { get; set; } = string.Empty;
        public int Total { get; set; }
        public int Converted { get; set; }
        public double RatePercentage { get; set; }
    }

    public class UserActivityReportViewModel : ReportFilterBase
    {
        public List<UserActivitySummary> UserSummaries { get; set; } = new();
    }

    public class UserActivitySummary
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int TotalActivities { get; set; }
        public int CallsCount { get; set; }
        public int MeetingsCount { get; set; }
        public int EmailsCount { get; set; }
        public int TasksCount { get; set; }
        public int CompletedFollowUps { get; set; }
    }

    public class AuditReportViewModel : ReportFilterBase
    {
        public string? ActionFilter { get; set; }
        public string? EntityFilter { get; set; }
        public List<AuditReportItem> Items { get; set; } = new();
    }

    public class AuditReportItem
    {
        public int AuditLogId { get; set; }
        public string? UserEmail { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string? RecordId { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? IpAddress { get; set; }
        public string? Details { get; set; }
    }
}
