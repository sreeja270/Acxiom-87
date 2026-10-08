using AcxiomCRM.Data;
using AcxiomCRM.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services
{
    public interface IReportService
    {
        Task<CustomerReportViewModel> GetCustomerReportAsync(UserContext userContext, string? status, string? ownerId, DateTime? start, DateTime? end);
        Task<LeadReportViewModel> GetLeadReportAsync(UserContext userContext, string? status, string? source, string? ownerId, DateTime? start, DateTime? end);
        Task<FollowUpReportViewModel> GetFollowUpReportAsync(UserContext userContext, string? status, string? ownerId, DateTime? start, DateTime? end);
        Task<OpportunityReportViewModel> GetOpportunityReportAsync(UserContext userContext, string? stage, string? status, string? ownerId, DateTime? start, DateTime? end);
        Task<PipelineReportViewModel> GetPipelineReportAsync(UserContext userContext, string? ownerId);
        Task<ConversionReportViewModel> GetConversionReportAsync(UserContext userContext, string? ownerId, DateTime? start, DateTime? end);
        Task<UserActivityReportViewModel> GetUserActivityReportAsync(UserContext userContext, DateTime? start, DateTime? end);
        Task<AuditReportViewModel> GetAuditReportAsync(UserContext userContext, string? action, string? entity, string? ownerId, DateTime? start, DateTime? end);
    }

    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;

        public ReportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<CustomerReportViewModel> GetCustomerReportAsync(UserContext userContext, string? status, string? ownerId, DateTime? start, DateTime? end)
        {
            var query = _context.Customers
                .Include(c => c.AssignedTo)
                .Include(c => c.Opportunities)
                .AsNoTracking()
                .AsQueryable();

            if (userContext.IsSalesExecutive)
            {
                query = query.Where(c => c.AssignedToId == userContext.UserId);
            }
            else if (!string.IsNullOrWhiteSpace(ownerId))
            {
                query = query.Where(c => c.AssignedToId == ownerId);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(c => c.Status == status);
            }

            if (start.HasValue)
            {
                query = query.Where(c => c.CreatedDate >= start.Value.ToUniversalTime());
            }

            if (end.HasValue)
            {
                query = query.Where(c => c.CreatedDate <= end.Value.Date.AddDays(1).AddTicks(-1).ToUniversalTime());
            }

            var list = await query.OrderBy(c => c.CustomerName).ToListAsync();

            var vm = new CustomerReportViewModel
            {
                StartDate = start,
                EndDate = end,
                AssignedToId = ownerId,
                StatusFilter = status,
                TotalActive = list.Count(c => c.Status == "Active"),
                TotalInactive = list.Count(c => c.Status == "Inactive"),
                Items = list.Select(c => new CustomerReportItem
                {
                    CustomerCode = c.CustomerCode,
                    CustomerName = c.CustomerName,
                    CompanyName = c.CompanyName,
                    Email = c.Email,
                    Phone = c.Phone,
                    Status = c.Status,
                    OwnerName = c.AssignedTo?.FullName ?? "Unassigned",
                    CreatedDate = c.CreatedDate,
                    OpportunityCount = c.Opportunities.Count,
                    TotalOpportunityValue = c.Opportunities.Sum(o => o.Amount)
                }).ToList()
            };

            return vm;
        }

        public async Task<LeadReportViewModel> GetLeadReportAsync(UserContext userContext, string? status, string? source, string? ownerId, DateTime? start, DateTime? end)
        {
            var query = _context.Leads
                .Include(l => l.AssignedTo)
                .AsNoTracking()
                .AsQueryable();

            if (userContext.IsSalesExecutive)
            {
                query = query.Where(l => l.AssignedToId == userContext.UserId);
            }
            else if (!string.IsNullOrWhiteSpace(ownerId))
            {
                query = query.Where(l => l.AssignedToId == ownerId);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(l => l.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(source))
            {
                query = query.Where(l => l.Source == source);
            }

            if (start.HasValue)
            {
                query = query.Where(l => l.CreatedDate >= start.Value.ToUniversalTime());
            }

            if (end.HasValue)
            {
                query = query.Where(l => l.CreatedDate <= end.Value.Date.AddDays(1).AddTicks(-1).ToUniversalTime());
            }

            var list = await query.OrderByDescending(l => l.CreatedDate).ToListAsync();

            var vm = new LeadReportViewModel
            {
                StartDate = start,
                EndDate = end,
                AssignedToId = ownerId,
                StatusFilter = status,
                SourceFilter = source,
                TotalLeads = list.Count,
                ConvertedCount = list.Count(l => l.Status == "Converted"),
                TotalExpectedValue = list.Sum(l => l.ExpectedValue),
                Items = list.Select(l => new LeadReportItem
                {
                    LeadCode = l.LeadCode,
                    LeadName = l.LeadName,
                    CompanyName = l.CompanyName,
                    Source = l.Source,
                    Status = l.Status,
                    ExpectedValue = l.ExpectedValue,
                    OwnerName = l.AssignedTo?.FullName ?? "Unassigned",
                    CreatedDate = l.CreatedDate,
                    IsConverted = l.Status == "Converted",
                    ConvertedDate = l.ConvertedDate
                }).ToList()
            };

            return vm;
        }

        public async Task<FollowUpReportViewModel> GetFollowUpReportAsync(UserContext userContext, string? status, string? ownerId, DateTime? start, DateTime? end)
        {
            var query = _context.FollowUps
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .Include(f => f.AssignedTo)
                .AsNoTracking()
                .AsQueryable();

            if (userContext.IsSalesExecutive)
            {
                query = query.Where(f => f.AssignedToId == userContext.UserId);
            }
            else if (!string.IsNullOrWhiteSpace(ownerId))
            {
                query = query.Where(f => f.AssignedToId == ownerId);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(f => f.Status == status);
            }

            if (start.HasValue)
            {
                query = query.Where(f => f.FollowUpDate >= start.Value.ToUniversalTime());
            }

            if (end.HasValue)
            {
                query = query.Where(f => f.FollowUpDate <= end.Value.Date.AddDays(1).AddTicks(-1).ToUniversalTime());
            }

            var list = await query.OrderBy(f => f.FollowUpDate).ToListAsync();
            var today = DateTime.Today;

            var vm = new FollowUpReportViewModel
            {
                StartDate = start,
                EndDate = end,
                AssignedToId = ownerId,
                StatusFilter = status,
                TotalCount = list.Count,
                OverdueCount = list.Count(f => f.Status == "Planned" && f.FollowUpDate.Date < today),
                CompletedCount = list.Count(f => f.Status == "Completed"),
                PlannedCount = list.Count(f => f.Status == "Planned"),
                Items = list.Select(f => new FollowUpReportItem
                {
                    FollowUpId = f.FollowUpId,
                    FollowUpDate = f.FollowUpDate,
                    FollowUpType = f.FollowUpType,
                    Status = f.Status,
                    CustomerOrLeadName = f.Customer?.CustomerName ?? f.Lead?.LeadName ?? "General",
                    Remarks = f.Remarks,
                    OwnerName = f.AssignedTo?.FullName ?? "Unassigned",
                    IsOverdue = f.Status == "Planned" && f.FollowUpDate.Date < today
                }).ToList()
            };

            return vm;
        }

        public async Task<OpportunityReportViewModel> GetOpportunityReportAsync(UserContext userContext, string? stage, string? status, string? ownerId, DateTime? start, DateTime? end)
        {
            var query = _context.Opportunities
                .Include(o => o.Customer)
                .Include(o => o.AssignedTo)
                .AsNoTracking()
                .AsQueryable();

            if (userContext.IsSalesExecutive)
            {
                query = query.Where(o => o.AssignedToId == userContext.UserId);
            }
            else if (!string.IsNullOrWhiteSpace(ownerId))
            {
                query = query.Where(o => o.AssignedToId == ownerId);
            }

            if (!string.IsNullOrWhiteSpace(stage))
            {
                query = query.Where(o => o.Stage == stage);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(o => o.Status == status);
            }

            if (start.HasValue)
            {
                query = query.Where(o => o.CreatedDate >= start.Value.ToUniversalTime());
            }

            if (end.HasValue)
            {
                query = query.Where(o => o.CreatedDate <= end.Value.Date.AddDays(1).AddTicks(-1).ToUniversalTime());
            }

            var rawList = await query.ToListAsync();
            var list = rawList.OrderByDescending(o => o.Amount).ToList();

            var vm = new OpportunityReportViewModel
            {
                StartDate = start,
                EndDate = end,
                AssignedToId = ownerId,
                StageFilter = stage,
                StatusFilter = status,
                TotalAmount = list.Sum(o => o.Amount),
                TotalWeightedAmount = Math.Round(list.Sum(o => o.Amount * (decimal)o.Probability / 100m), 2),
                WonCount = list.Count(o => o.Status == "Won"),
                LostCount = list.Count(o => o.Status == "Lost"),
                Items = list.Select(o => new OpportunityReportItem
                {
                    OpportunityCode = o.OpportunityCode,
                    OpportunityName = o.OpportunityName,
                    CustomerName = o.Customer?.CustomerName ?? "Unknown",
                    Stage = o.Stage,
                    Amount = o.Amount,
                    Probability = o.Probability,
                    WeightedAmount = Math.Round(o.Amount * (decimal)o.Probability / 100m, 2),
                    ExpectedCloseDate = o.ExpectedCloseDate,
                    Status = o.Status,
                    OwnerName = o.AssignedTo?.FullName ?? "Unassigned"
                }).ToList()
            };

            return vm;
        }

        public async Task<PipelineReportViewModel> GetPipelineReportAsync(UserContext userContext, string? ownerId)
        {
            var query = _context.Opportunities
                .Include(o => o.AssignedTo)
                .Where(o => o.Status == "Open")
                .AsNoTracking()
                .AsQueryable();

            if (userContext.IsSalesExecutive)
            {
                query = query.Where(o => o.AssignedToId == userContext.UserId);
            }
            else if (!string.IsNullOrWhiteSpace(ownerId))
            {
                query = query.Where(o => o.AssignedToId == ownerId);
            }

            var openOpps = await query.ToListAsync();

            var vm = new PipelineReportViewModel
            {
                AssignedToId = ownerId,
                GrandTotalPipeline = openOpps.Sum(o => o.Amount),
                GrandTotalWeighted = Math.Round(openOpps.Sum(o => o.Amount * (decimal)o.Probability / 100m), 2)
            };

            // Stage breakdown
            string[] stages = ["Qualification", "Proposal", "Negotiation"];
            foreach (var s in stages)
            {
                var stageItems = openOpps.Where(o => o.Stage == s).ToList();
                vm.StageBreakdown.Add(new StagePipelineSummary
                {
                    Stage = s,
                    Count = stageItems.Count,
                    TotalAmount = stageItems.Sum(o => o.Amount),
                    WeightedAmount = Math.Round(stageItems.Sum(o => o.Amount * (decimal)o.Probability / 100m), 2)
                });
            }

            // Owner breakdown
            var ownerGroups = openOpps.GroupBy(o => o.AssignedTo?.FullName ?? "Unassigned");
            foreach (var g in ownerGroups)
            {
                vm.OwnerBreakdown.Add(new OwnerPipelineSummary
                {
                    OwnerName = g.Key,
                    Count = g.Count(),
                    TotalAmount = g.Sum(o => o.Amount),
                    WeightedAmount = Math.Round(g.Sum(o => o.Amount * (decimal)o.Probability / 100m), 2)
                });
            }

            return vm;
        }

        public async Task<ConversionReportViewModel> GetConversionReportAsync(UserContext userContext, string? ownerId, DateTime? start, DateTime? end)
        {
            var leadQuery = _context.Leads.AsNoTracking().AsQueryable();
            var oppQuery = _context.Opportunities.AsNoTracking().AsQueryable();

            if (userContext.IsSalesExecutive)
            {
                leadQuery = leadQuery.Where(l => l.AssignedToId == userContext.UserId);
                oppQuery = oppQuery.Where(o => o.AssignedToId == userContext.UserId);
            }
            else if (!string.IsNullOrWhiteSpace(ownerId))
            {
                leadQuery = leadQuery.Where(l => l.AssignedToId == ownerId);
                oppQuery = oppQuery.Where(o => o.AssignedToId == ownerId);
            }

            if (start.HasValue)
            {
                leadQuery = leadQuery.Where(l => l.CreatedDate >= start.Value.ToUniversalTime());
                oppQuery = oppQuery.Where(o => o.CreatedDate >= start.Value.ToUniversalTime());
            }

            if (end.HasValue)
            {
                var endUtc = end.Value.Date.AddDays(1).AddTicks(-1).ToUniversalTime();
                leadQuery = leadQuery.Where(l => l.CreatedDate <= endUtc);
                oppQuery = oppQuery.Where(o => o.CreatedDate <= endUtc);
            }

            var leads = await leadQuery.ToListAsync();
            var opps = await oppQuery.ToListAsync();

            var totalLeads = leads.Count;
            var converted = leads.Count(l => l.Status == "Converted");
            var unconverted = totalLeads - converted;
            var convRate = totalLeads > 0 ? Math.Round((double)converted / totalLeads * 100, 1) : 0.0;

            var wonOpps = opps.Where(o => o.Status == "Won").ToList();
            var lostOpps = opps.Where(o => o.Status == "Lost").ToList();
            var closedOppsCount = wonOpps.Count + lostOpps.Count;
            var winRate = closedOppsCount > 0 ? Math.Round((double)wonOpps.Count / closedOppsCount * 100, 1) : 0.0;

            var vm = new ConversionReportViewModel
            {
                StartDate = start,
                EndDate = end,
                AssignedToId = ownerId,
                TotalLeads = totalLeads,
                ConvertedLeads = converted,
                UnconvertedLeads = unconverted,
                ConversionRatePercentage = convRate,
                TotalWonRevenue = wonOpps.Sum(o => o.Amount),
                TotalLostRevenue = lostOpps.Sum(o => o.Amount),
                OpportunityWinRatePercentage = winRate
            };

            var sourceGroups = leads.GroupBy(l => l.Source);
            foreach (var g in sourceGroups)
            {
                var gTotal = g.Count();
                var gConv = g.Count(l => l.Status == "Converted");
                vm.BySource.Add(new SourceConversionSummary
                {
                    Source = g.Key,
                    Total = gTotal,
                    Converted = gConv,
                    RatePercentage = gTotal > 0 ? Math.Round((double)gConv / gTotal * 100, 1) : 0.0
                });
            }

            return vm;
        }

        public async Task<UserActivityReportViewModel> GetUserActivityReportAsync(UserContext userContext, DateTime? start, DateTime? end)
        {
            var usersQuery = _context.Users.AsNoTracking().AsQueryable();
            if (userContext.IsSalesExecutive)
            {
                usersQuery = usersQuery.Where(u => u.Id == userContext.UserId);
            }

            var users = await usersQuery.ToListAsync();
            var activitiesQuery = _context.Activities.AsNoTracking().AsQueryable();
            var followUpsQuery = _context.FollowUps.Where(f => f.Status == "Completed").AsNoTracking().AsQueryable();

            if (start.HasValue)
            {
                activitiesQuery = activitiesQuery.Where(a => a.ActivityDate >= start.Value.ToUniversalTime());
                followUpsQuery = followUpsQuery.Where(f => f.CompletedDate >= start.Value.ToUniversalTime());
            }

            if (end.HasValue)
            {
                var endUtc = end.Value.Date.AddDays(1).AddTicks(-1).ToUniversalTime();
                activitiesQuery = activitiesQuery.Where(a => a.ActivityDate <= endUtc);
                followUpsQuery = followUpsQuery.Where(f => f.CompletedDate <= endUtc);
            }

            var activities = await activitiesQuery.ToListAsync();
            var followUps = await followUpsQuery.ToListAsync();

            var vm = new UserActivityReportViewModel
            {
                StartDate = start,
                EndDate = end
            };

            foreach (var user in users)
            {
                var userActs = activities.Where(a => a.AssignedToId == user.Id).ToList();
                var userFups = followUps.Where(f => f.AssignedToId == user.Id).ToList();

                vm.UserSummaries.Add(new UserActivitySummary
                {
                    UserId = user.Id,
                    UserName = user.FullName,
                    Role = user.Department ?? "Sales",
                    TotalActivities = userActs.Count,
                    CallsCount = userActs.Count(a => a.ActivityType == "Call"),
                    MeetingsCount = userActs.Count(a => a.ActivityType == "Meeting"),
                    EmailsCount = userActs.Count(a => a.ActivityType == "Email"),
                    TasksCount = userActs.Count(a => a.ActivityType == "Task"),
                    CompletedFollowUps = userFups.Count
                });
            }

            return vm;
        }

        public async Task<AuditReportViewModel> GetAuditReportAsync(UserContext userContext, string? action, string? entity, string? ownerId, DateTime? start, DateTime? end)
        {
            if (userContext.IsSalesExecutive)
            {
                throw new UnauthorizedAccessException("Sales Executives are not authorized to view audit reports.");
            }

            var query = _context.AuditLogs.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(action))
            {
                query = query.Where(a => a.Action == action);
            }

            if (!string.IsNullOrWhiteSpace(entity))
            {
                query = query.Where(a => a.EntityName == entity);
            }

            if (!string.IsNullOrWhiteSpace(ownerId))
            {
                query = query.Where(a => a.UserId == ownerId);
            }

            if (start.HasValue)
            {
                query = query.Where(a => a.CreatedDate >= start.Value.ToUniversalTime());
            }

            if (end.HasValue)
            {
                var endUtc = end.Value.Date.AddDays(1).AddTicks(-1).ToUniversalTime();
                query = query.Where(a => a.CreatedDate <= endUtc);
            }

            var list = await query.OrderByDescending(a => a.CreatedDate).Take(200).ToListAsync();

            return new AuditReportViewModel
            {
                ActionFilter = action,
                EntityFilter = entity,
                AssignedToId = ownerId,
                StartDate = start,
                EndDate = end,
                Items = list.Select(a => new AuditReportItem
                {
                    AuditLogId = a.AuditLogId,
                    UserEmail = a.UserEmail,
                    Action = a.Action,
                    EntityName = a.EntityName,
                    RecordId = a.RecordId,
                    CreatedDate = a.CreatedDate,
                    IpAddress = a.IpAddress,
                    Details = a.Details
                }).ToList()
            };
        }
    }
}
