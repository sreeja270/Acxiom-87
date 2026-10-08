using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models;
using AcxiomCRM.ViewModels.Common;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.ViewModels
{
    public class CustomerListViewModel
    {
        public PaginatedList<Customer> Customers { get; set; } = null!;
        public string? SearchTerm { get; set; }
        public string? StatusFilter { get; set; }
        public string? AssignedToFilter { get; set; }
        public string? SortOrder { get; set; }
        public int PageIndex { get; set; } = 1;

        public SelectList? UsersList { get; set; }
    }

    public class CustomerDetailsViewModel
    {
        public Customer Customer { get; set; } = null!;
        public List<Lead> RelatedLeads { get; set; } = new();
        public List<Opportunity> RelatedOpportunities { get; set; } = new();
        public List<FollowUp> RelatedFollowUps { get; set; } = new();
        public List<Activity> RelatedActivities { get; set; } = new();
        public List<AuditLog> AuditHistory { get; set; } = new();
        public decimal TotalOpportunityValue { get; set; }
        public decimal TotalWonValue { get; set; }
    }

    public class CustomerFormViewModel
    {
        public int CustomerId { get; set; }

        public string? CustomerCode { get; set; }

        [Required(ErrorMessage = "Customer Name is required.")]
        [StringLength(150, ErrorMessage = "Customer Name cannot exceed 150 characters.")]
        [Display(Name = "Customer / Contact Name")]
        public string CustomerName { get; set; } = string.Empty;

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

        [StringLength(250)]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        public string? State { get; set; }

        [Required]
        public string Status { get; set; } = "Active";

        [Display(Name = "Assigned Sales Executive")]
        public string? AssignedToId { get; set; }

        public SelectList? UsersList { get; set; }
    }
}
