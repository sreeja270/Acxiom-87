using AcxiomCRM.Models;

namespace AcxiomCRM.ViewModels
{
    public class DashboardViewModel
    {
        // 8 Mandatory KPI Cards
        public int TotalCustomers { get; set; }
        public int TotalLeads { get; set; }
        public int OpenLeads { get; set; }
        public int TotalOpportunities { get; set; }
        public int OpenOpportunities { get; set; }
        public int WonOpportunities { get; set; }
        public int LostOpportunities { get; set; }
        public decimal TotalPipelineValue { get; set; }
        public decimal WeightedPipelineValue { get; set; }

        // Date Filter (Today, This Week, This Month, Custom Range)
        public string DateFilter { get; set; } = "This Month";
        public DateTime? CustomStartDate { get; set; }
        public DateTime? CustomEndDate { get; set; }

        // User Context
        public string UserRole { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;

        // Chart Data (authorized server-side data)
        // Chart 1: Lead Status (New, Contacted, Qualified, Lost, Converted)
        public Dictionary<string, int> LeadStatusCounts { get; set; } = new();

        // Chart 2: Opportunity Pipeline (Qualification, Proposal, Negotiation, Won, Lost)
        public Dictionary<string, int> OpportunityStageCounts { get; set; } = new();
        public Dictionary<string, decimal> OpportunityStageAmounts { get; set; } = new();

        // Chart 3: Monthly Sales (Monthly pipeline/outcomes totals)
        public List<MonthlySalesTrendItem> MonthlySalesTrends { get; set; } = new();

        // Quick Overview Tables
        public List<FollowUp> UpcomingFollowUps { get; set; } = new();
        public List<Activity> RecentActivities { get; set; } = new();
        public List<Opportunity> TopOpportunities { get; set; } = new();
        public List<Lead> RecentLeads { get; set; } = new();
    }

    public class MonthlySalesTrendItem
    {
        public string MonthLabel { get; set; } = string.Empty;
        public decimal WonAmount { get; set; }
        public decimal PipelineAmount { get; set; }
    }
}
