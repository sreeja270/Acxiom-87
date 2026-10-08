using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services
{
    public interface IDashboardService
    {
        Task<DashboardViewModel> GetDashboardDataAsync(UserContext userContext, string dateFilter, DateTime? customStart, DateTime? customEnd);
    }

    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;

        public DashboardService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardViewModel> GetDashboardDataAsync(UserContext userContext, string dateFilter, DateTime? customStart, DateTime? customEnd)
        {
            var vm = new DashboardViewModel
            {
                DateFilter = dateFilter,
                CustomStartDate = customStart,
                CustomEndDate = customEnd,
                UserRole = userContext.Role,
                UserName = userContext.UserName,
                UserEmail = userContext.UserEmail
            };

            // Calculate date bounds
            DateTime? startDate = null;
            DateTime? endDate = null;

            var today = DateTime.Today;
            if (dateFilter == "Today")
            {
                startDate = today;
                endDate = today.AddDays(1);
            }
            else if (dateFilter == "This Week")
            {
                var diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
                startDate = today.AddDays(-1 * diff);
                endDate = startDate.Value.AddDays(7);
            }
            else if (dateFilter == "This Month")
            {
                startDate = new DateTime(today.Year, today.Month, 1);
                endDate = startDate.Value.AddMonths(1);
            }
            else if (dateFilter == "Custom Range" && customStart.HasValue)
            {
                startDate = customStart.Value.Date;
                endDate = (customEnd ?? customStart.Value).Date.AddDays(1);
            }

            // 1. Customers query with user scope
            var custQuery = _context.Customers.AsNoTracking();
            if (userContext.IsSalesExecutive)
            {
                custQuery = custQuery.Where(c => c.AssignedToId == userContext.UserId);
            }
            if (startDate.HasValue && endDate.HasValue)
            {
                // For KPI cards, show active or created in timeframe
                vm.TotalCustomers = await custQuery.CountAsync(c => c.CreatedDate >= startDate.Value && c.CreatedDate < endDate.Value);
            }
            else
            {
                vm.TotalCustomers = await custQuery.CountAsync();
            }

            // 2. Leads query with user scope
            var leadQuery = _context.Leads.AsNoTracking();
            if (userContext.IsSalesExecutive)
            {
                leadQuery = leadQuery.Where(l => l.AssignedToId == userContext.UserId);
            }

            var allLeads = await leadQuery.ToListAsync();
            vm.TotalLeads = allLeads.Count;
            vm.OpenLeads = allLeads.Count(l => l.Status != "Converted" && l.Status != "Lost");

            // Chart 1: Lead Status counts
            string[] leadStatuses = ["New", "Contacted", "Qualified", "Converted", "Lost", "Unqualified"];
            foreach (var status in leadStatuses)
            {
                vm.LeadStatusCounts[status] = allLeads.Count(l => l.Status == status);
            }

            // 3. Opportunities query with user scope
            var oppQuery = _context.Opportunities.AsNoTracking();
            if (userContext.IsSalesExecutive)
            {
                oppQuery = oppQuery.Where(o => o.AssignedToId == userContext.UserId);
            }

            var allOpps = await oppQuery.ToListAsync();
            vm.TotalOpportunities = allOpps.Count;
            vm.OpenOpportunities = allOpps.Count(o => o.Status == "Open");
            vm.WonOpportunities = allOpps.Count(o => o.Status == "Won");
            vm.LostOpportunities = allOpps.Count(o => o.Status == "Lost");

            var openOpps = allOpps.Where(o => o.Status == "Open").ToList();
            vm.TotalPipelineValue = openOpps.Sum(o => o.Amount);
            vm.WeightedPipelineValue = Math.Round(openOpps.Sum(o => o.Amount * (decimal)o.Probability / 100m), 2);

            // Chart 2: Opportunity Stage counts & amounts
            string[] oppStages = ["Qualification", "Proposal", "Negotiation", "Won", "Lost"];
            foreach (var stage in oppStages)
            {
                var stageOpps = allOpps.Where(o => o.Stage == stage).ToList();
                vm.OpportunityStageCounts[stage] = stageOpps.Count;
                vm.OpportunityStageAmounts[stage] = stageOpps.Sum(o => o.Amount);
            }

            // Chart 3: Monthly Sales (Last 6 months)
            for (int i = 5; i >= 0; i--)
            {
                var monthDate = DateTime.Today.AddMonths(-i);
                var monthStart = new DateTime(monthDate.Year, monthDate.Month, 1);
                var monthEnd = monthStart.AddMonths(1);

                var monthWon = allOpps
                    .Where(o => o.Stage == "Won" && o.CreatedDate >= monthStart && o.CreatedDate < monthEnd)
                    .Sum(o => o.Amount);

                var monthPipeline = allOpps
                    .Where(o => o.CreatedDate >= monthStart && o.CreatedDate < monthEnd)
                    .Sum(o => o.Amount);

                vm.MonthlySalesTrends.Add(new MonthlySalesTrendItem
                {
                    MonthLabel = monthDate.ToString("MMM yyyy"),
                    WonAmount = monthWon,
                    PipelineAmount = monthPipeline
                });
            }

            // 4. Upcoming Follow-ups (Next 7 days, planned)
            var followUpQuery = _context.FollowUps
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .Include(f => f.AssignedTo)
                .Where(f => f.Status == "Planned" && f.FollowUpDate >= DateTime.Today)
                .AsNoTracking();

            if (userContext.IsSalesExecutive)
            {
                followUpQuery = followUpQuery.Where(f => f.AssignedToId == userContext.UserId);
            }
            vm.UpcomingFollowUps = await followUpQuery.OrderBy(f => f.FollowUpDate).Take(5).ToListAsync();

            // 5. Recent Activities
            var actQuery = _context.Activities
                .Include(a => a.Customer)
                .Include(a => a.Lead)
                .Include(a => a.AssignedTo)
                .AsNoTracking();

            if (userContext.IsSalesExecutive)
            {
                actQuery = actQuery.Where(a => a.AssignedToId == userContext.UserId);
            }
            vm.RecentActivities = await actQuery.OrderByDescending(a => a.ActivityDate).Take(5).ToListAsync();

            // 6. Top Opportunities
            var topOppQuery = _context.Opportunities
                .Include(o => o.Customer)
                .Include(o => o.AssignedTo)
                .Where(o => o.Status == "Open")
                .AsNoTracking();

            if (userContext.IsSalesExecutive)
            {
                topOppQuery = topOppQuery.Where(o => o.AssignedToId == userContext.UserId);
            }
            var openOppsList = await topOppQuery.ToListAsync();
            vm.TopOpportunities = openOppsList.OrderByDescending(o => o.Amount).Take(5).ToList();

            // 7. Recent Leads
            var recLeadQuery = _context.Leads
                .Include(l => l.AssignedTo)
                .AsNoTracking();

            if (userContext.IsSalesExecutive)
            {
                recLeadQuery = recLeadQuery.Where(l => l.AssignedToId == userContext.UserId);
            }
            vm.RecentLeads = await recLeadQuery.OrderByDescending(l => l.CreatedDate).Take(5).ToListAsync();

            return vm;
        }
    }
}
