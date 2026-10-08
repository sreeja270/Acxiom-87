using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models
{
    public class Opportunity
    {
        [Key]
        public int OpportunityId { get; set; }

        [Required]
        [StringLength(50)]
        public string OpportunityCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Opportunity Name is required.")]
        [StringLength(150, ErrorMessage = "Opportunity Name cannot exceed 150 characters.")]
        public string OpportunityName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Customer is required.")]
        public int CustomerId { get; set; }
        [ForeignKey(nameof(CustomerId))]
        public virtual Customer? Customer { get; set; }

        public int? LeadId { get; set; }
        [ForeignKey(nameof(LeadId))]
        public virtual Lead? Lead { get; set; }

        [Required(ErrorMessage = "Opportunity Amount must be greater than 0.")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Stage is required.")]
        [StringLength(50)]
        public string Stage { get; set; } = "Qualification"; // Qualification, Proposal, Negotiation, Won, Lost

        [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
        public int Probability { get; set; } = 20;

        [Required(ErrorMessage = "Expected Close Date is required.")]
        public DateTime ExpectedCloseDate { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Open"; // Open, Won, Lost

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [StringLength(150)]
        public string? CreatedBy { get; set; }

        public string? AssignedToId { get; set; }
        [ForeignKey(nameof(AssignedToId))]
        public virtual ApplicationUser? AssignedTo { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        [NotMapped]
        public decimal WeightedPipeline => Math.Round(Amount * (decimal)Probability / 100m, 2);
    }
}
