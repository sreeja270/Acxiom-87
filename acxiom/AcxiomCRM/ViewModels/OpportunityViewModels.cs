using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models;
using AcxiomCRM.ViewModels.Common;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.ViewModels
{
    public class OpportunityListViewModel
    {
        public PaginatedList<Opportunity> Opportunities { get; set; } = null!;
        public string? SearchTerm { get; set; }
        public string? StageFilter { get; set; }
        public string? StatusFilter { get; set; }
        public string? AssignedToFilter { get; set; }
        public string? SortOrder { get; set; }
        public int PageIndex { get; set; } = 1;

        public decimal TotalPipelineValue { get; set; }
        public decimal TotalWeightedPipelineValue { get; set; }

        public SelectList? UsersList { get; set; }
    }

    public class OpportunityDetailsViewModel
    {
        public Opportunity Opportunity { get; set; } = null!;
        public List<FollowUp> RelatedFollowUps { get; set; } = new();
        public List<Activity> RelatedActivities { get; set; } = new();
        public List<AuditLog> AuditHistory { get; set; } = new();
    }

    public class OpportunityFormViewModel : IValidatableObject
    {
        public int OpportunityId { get; set; }

        public string? OpportunityCode { get; set; }

        [Required(ErrorMessage = "Opportunity Name is required.")]
        [StringLength(150, ErrorMessage = "Opportunity Name cannot exceed 150 characters.")]
        [Display(Name = "Opportunity Name")]
        public string OpportunityName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Customer is required.")]
        [Display(Name = "Customer")]
        public int CustomerId { get; set; }

        [Display(Name = "Originating Lead (Optional)")]
        public int? LeadId { get; set; }

        [Required(ErrorMessage = "Opportunity Amount must be greater than 0.")]
        [Range(0.01, 999999999.99, ErrorMessage = "Opportunity Amount must be greater than 0.")]
        [Display(Name = "Amount ($)")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Stage is required.")]
        [Display(Name = "Pipeline Stage")]
        public string Stage { get; set; } = "Qualification"; // Qualification, Proposal, Negotiation, Won, Lost

        [Required(ErrorMessage = "Probability must be between 0 and 100.")]
        [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
        [Display(Name = "Win Probability (%)")]
        public int Probability { get; set; } = 20;

        [Required(ErrorMessage = "Expected Close Date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Expected Close Date")]
        public DateTime ExpectedCloseDate { get; set; } = DateTime.Today.AddDays(30);

        [Required]
        public string Status { get; set; } = "Open"; // Open, Won, Lost

        [Display(Name = "Assigned Sales Executive")]
        public string? AssignedToId { get; set; }

        [StringLength(1000)]
        [Display(Name = "Notes / Description")]
        public string? Description { get; set; }

        public SelectList? CustomersList { get; set; }
        public SelectList? LeadsList { get; set; }
        public SelectList? UsersList { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Status == "Open" && ExpectedCloseDate.Date < DateTime.Today)
            {
                yield return new ValidationResult(
                    "Expected Close Date cannot be in the past.",
                    new[] { nameof(ExpectedCloseDate) });
            }

            if (Status == "Open" && Amount <= 0)
            {
                yield return new ValidationResult(
                    "Opportunity Amount must be greater than 0.",
                    new[] { nameof(Amount) });
            }
        }
    }
}
