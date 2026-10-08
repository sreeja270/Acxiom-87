using AcxiomCRM.Models;
using AcxiomCRM.ViewModels.Common;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.ViewModels
{
    public class AuditLogListViewModel
    {
        public PaginatedList<AuditLog> AuditLogs { get; set; } = null!;
        public string? SearchTerm { get; set; }
        public string? ActionFilter { get; set; }
        public string? EntityFilter { get; set; }
        public string? UserIdFilter { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int PageIndex { get; set; } = 1;

        public SelectList? UsersList { get; set; }
        public SelectList? ActionsList { get; set; }
        public SelectList? EntitiesList { get; set; }
    }
}
