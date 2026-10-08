using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models
{
    public class FollowUp
    {
        [Key]
        public int FollowUpId { get; set; }

        public int? CustomerId { get; set; }
        [ForeignKey(nameof(CustomerId))]
        public virtual Customer? Customer { get; set; }

        public int? LeadId { get; set; }
        [ForeignKey(nameof(LeadId))]
        public virtual Lead? Lead { get; set; }

        [Required(ErrorMessage = "Follow-up Date is required.")]
        public DateTime FollowUpDate { get; set; }

        [Required(ErrorMessage = "Follow-up Type is required.")]
        [StringLength(50)]
        public string FollowUpType { get; set; } = "Call"; // Call, Meeting, Email, Demo, Check-in

        [Required(ErrorMessage = "Remarks are required.")]
        [StringLength(1000)]
        public string Remarks { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Planned"; // Planned, Completed, Missed, Cancelled

        [Required]
        public string AssignedToId { get; set; } = string.Empty;
        [ForeignKey(nameof(AssignedToId))]
        public virtual ApplicationUser? AssignedTo { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [StringLength(150)]
        public string? CreatedBy { get; set; }

        public DateTime? CompletedDate { get; set; }

        [StringLength(1000)]
        public string? CompletionNotes { get; set; }
    }
}
