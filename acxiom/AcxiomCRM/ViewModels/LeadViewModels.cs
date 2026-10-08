using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models;
using AcxiomCRM.ViewModels.Common;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.ViewModels
{
    public class LeadListViewModel
    {
        public PaginatedList<Lead> Leads { get; set; } = null!;
        public string? SearchTerm { get; set; }
        public string? StatusFilter { get; set; }
        public string? SourceFilter { get; set; }
        public string? PriorityFilter { get; set; }
        public string? AssignedToFilter { get; set; }
        public string? SortOrder { get; set; }
        public int PageIndex { get; set; } = 1;

        public SelectList? UsersList { get; set; }
    }

    public class LeadDetailsViewModel
    {
        public Lead Lead { get; set; } = null!;
        public Customer? ConvertedCustomer { get; set; }
        public List<Opportunity> RelatedOpportunities { get; set; } = new();
        public List<FollowUp> RelatedFollowUps { get; set; } = new();
        public List<Activity> RelatedActivities { get; set; } = new();
        public List<AuditLog> AuditHistory { get; set; } = new();
    }

    public class LeadFormViewModel
    {
        public int LeadId { get; set; }

        public string? LeadCode { get; set; }

        [Required(ErrorMessage = "Lead name is required.")]
        [StringLength(150, ErrorMessage = "Lead Name cannot exceed 150 characters.")]
        [Display(Name = "Lead Contact Name")]
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
        [Display(Name = "Company Name")]
        public string? CompanyName { get; set; }

        [Required(ErrorMessage = "Lead source is required.")]
        public string Source { get; set; } = "Website";

        [Required(ErrorMessage = "Lead status is required.")]
        public string Status { get; set; } = "New";

        [Required]
        public string Priority { get; set; } = "Medium";

        [Range(0, 999999999.99, ErrorMessage = "Expected value must be numeric and greater than or equal to 0.")]
        [Display(Name = "Expected Value ($)")]
        public decimal ExpectedValue { get; set; } = 0.00m;

        [Display(Name = "Assigned Sales Executive")]
        public string? AssignedToId { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        public SelectList? UsersList { get; set; }
    }

    public class ConvertLeadViewModel
    {
        public int LeadId { get; set; }
        public string LeadCode { get; set; } = string.Empty;
        public string LeadName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? CompanyName { get; set; }

        [Display(Name = "Create Linked Opportunity")]
        public bool CreateOpportunity { get; set; } = true;

        [Display(Name = "Opportunity Name")]
        [StringLength(150)]
        public string? OpportunityName { get; set; }

        [Display(Name = "Opportunity Amount ($)")]
        [Range(1, 999999999.99, ErrorMessage = "Opportunity Amount must be greater than 0.")]
        public decimal OpportunityAmount { get; set; }

        [Display(Name = "Expected Close Date")]
        public DateTime? ExpectedCloseDate { get; set; }

        [Display(Name = "Initial Probability (%)")]
        [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
        public int Probability { get; set; } = 25;

        [Display(Name = "Assigned Sales Executive")]
        public string? AssignedToId { get; set; }

        public SelectList? UsersList { get; set; }
    }
}
