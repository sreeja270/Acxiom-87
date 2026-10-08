using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models
{
    public class Lead
    {
        [Key]
        public int LeadId { get; set; }

        [Required]
        [StringLength(50)]
        public string LeadCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Lead name is required.")]
        [StringLength(150, ErrorMessage = "Lead Name cannot exceed 150 characters.")]
        public string LeadName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter a valid email address.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter a valid phone number.")]
        [RegularExpression(@"^(\+?[0-9\s\-\(\)]{7,20})$", ErrorMessage = "Enter a valid phone number.")]
        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        [StringLength(150)]
        public string? CompanyName { get; set; }

        [Required(ErrorMessage = "Lead source is required.")]
        [StringLength(100)]
        public string Source { get; set; } = "Website"; // Website, Referral, Cold Call, Event, Partner, Social Media

        [Required(ErrorMessage = "Lead status is required.")]
        [StringLength(50)]
        public string Status { get; set; } = "New"; // New, Contacted, Qualified, Unqualified, Converted, Lost

        [Required]
        [StringLength(50)]
        public string Priority { get; set; } = "Medium"; // Low, Medium, High, Critical

        [Range(0, 999999999.99, ErrorMessage = "Expected value must be greater than or equal to 0.")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ExpectedValue { get; set; } = 0.00m;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [StringLength(150)]
        public string? CreatedBy { get; set; }

        public string? AssignedToId { get; set; }
        [ForeignKey(nameof(AssignedToId))]
        public virtual ApplicationUser? AssignedTo { get; set; }

        public int? CustomerId { get; set; }
        [ForeignKey(nameof(CustomerId))]
        public virtual Customer? Customer { get; set; }

        public DateTime? ConvertedDate { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        // Navigations
        public virtual ICollection<Opportunity> Opportunities { get; set; } = new List<Opportunity>();
        public virtual ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
        public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();
    }
}
