using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.ViewModels.Common;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services
{
    public interface IAuditService
    {
        Task LogAsync(string? userId, string? userEmail, string action, string entityName, string? recordId, string? oldValue, string? newValue, string? details, string? ipAddress);
        Task<PaginatedList<AuditLog>> GetAuditLogsAsync(UserContext userContext, string? searchTerm, string? actionFilter, string? entityFilter, string? userIdFilter, DateTime? startDate, DateTime? endDate, int pageIndex, int pageSize);
        Task<List<AuditLog>> GetRecentLogsForEntityAsync(string entityName, string recordId, int limit = 10);
    }

    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuditService> _logger;

        public AuditService(ApplicationDbContext context, ILogger<AuditService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task LogAsync(
            string? userId,
            string? userEmail,
            string action,
            string entityName,
            string? recordId,
            string? oldValue,
            string? newValue,
            string? details,
            string? ipAddress)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    UserId = userId,
                    UserEmail = userEmail,
                    Action = action,
                    EntityName = entityName,
                    RecordId = recordId,
                    OldValue = oldValue,
                    NewValue = newValue,
                    Details = details,
                    IpAddress = ipAddress,
                    CreatedDate = DateTime.UtcNow
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Never let audit log failures crash the main flow, but log securely on server
                _logger.LogError(ex, "Failed to record audit log for action: {Action}, entity: {Entity}", action, entityName);
            }
        }

        public async Task<PaginatedList<AuditLog>> GetAuditLogsAsync(
            UserContext userContext,
            string? searchTerm,
            string? actionFilter,
            string? entityFilter,
            string? userIdFilter,
            DateTime? startDate,
            DateTime? endDate,
            int pageIndex,
            int pageSize)
        {
            // Only Admin (or Manager with restricted scope) can access audit logs. Sales Executive has no access!
            if (userContext.IsSalesExecutive)
            {
                throw new UnauthorizedAccessException("Sales Executives are not authorized to access audit logs.");
            }

            var query = _context.AuditLogs
                .Include(a => a.User)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(a =>
                    (a.UserEmail != null && a.UserEmail.ToLower().Contains(term)) ||
                    a.Action.ToLower().Contains(term) ||
                    a.EntityName.ToLower().Contains(term) ||
                    (a.RecordId != null && a.RecordId.ToLower().Contains(term)) ||
                    (a.Details != null && a.Details.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(actionFilter))
            {
                query = query.Where(a => a.Action == actionFilter);
            }

            if (!string.IsNullOrWhiteSpace(entityFilter))
            {
                query = query.Where(a => a.EntityName == entityFilter);
            }

            if (!string.IsNullOrWhiteSpace(userIdFilter))
            {
                query = query.Where(a => a.UserId == userIdFilter);
            }

            if (startDate.HasValue)
            {
                query = query.Where(a => a.CreatedDate >= startDate.Value.ToUniversalTime());
            }

            if (endDate.HasValue)
            {
                var endUtc = endDate.Value.Date.AddDays(1).AddTicks(-1).ToUniversalTime();
                query = query.Where(a => a.CreatedDate <= endUtc);
            }

            query = query.OrderByDescending(a => a.CreatedDate);

            return await PaginatedList<AuditLog>.CreateAsync(query, pageIndex, pageSize);
        }

        public async Task<List<AuditLog>> GetRecentLogsForEntityAsync(string entityName, string recordId, int limit = 10)
        {
            return await _context.AuditLogs
                .AsNoTracking()
                .Where(a => a.EntityName == entityName && a.RecordId == recordId)
                .OrderByDescending(a => a.CreatedDate)
                .Take(limit)
                .ToListAsync();
        }
    }
}
