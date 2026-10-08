using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models
{
    public class Activity
    {
        [Key]
        public int ActivityId { get; set; }

        [Required(ErrorMessage = "Activity Type is required.")]
        [StringLength(50)]
        public string ActivityType { get; set; } = "Call"; // Call, Meeting, Email, Task

        [Required(ErrorMessage = "Subject is required.")]
        [StringLength(150, ErrorMessage = "Subject cannot exceed 150 characters.")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required.")]
        [StringLength(1500)]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Activity Date is required.")]
        public DateTime ActivityDate { get; set; }

        public int? CustomerId { get; set; }
        [ForeignKey(nameof(CustomerId))]
        public virtual Customer? Customer { get; set; }

        public int? LeadId { get; set; }
        [ForeignKey(nameof(LeadId))]
        public virtual Lead? Lead { get; set; }

        [Required]
        public string AssignedToId { get; set; } = string.Empty;
        [ForeignKey(nameof(AssignedToId))]
        public virtual ApplicationUser? AssignedTo { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Completed"; // Pending, In Progress, Completed, Cancelled

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [StringLength(150)]
        public string? CreatedBy { get; set; }
    }
}
