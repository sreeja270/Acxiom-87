using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models
{
    public class AuditLog
    {
        [Key]
        public int AuditLogId { get; set; }

        public string? UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser? User { get; set; }

        [StringLength(150)]
        public string? UserEmail { get; set; }

        [Required]
        [StringLength(100)]
        public string Action { get; set; } = string.Empty; // Create, Update, Delete, Login, Logout, Failed Login, Lockout, Unlock, Lead Conversion, Role Change

        [Required]
        [StringLength(100)]
        public string EntityName { get; set; } = string.Empty; // Customer, Lead, Opportunity, FollowUp, Activity, User, Role, Auth

        [StringLength(100)]
        public string? RecordId { get; set; }

        public string? OldValue { get; set; }

        public string? NewValue { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [StringLength(50)]
        public string? IpAddress { get; set; }

        [StringLength(1000)]
        public string? Details { get; set; }
    }
}
