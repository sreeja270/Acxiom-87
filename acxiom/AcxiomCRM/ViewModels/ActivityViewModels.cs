using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models;
using AcxiomCRM.ViewModels.Common;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.ViewModels
{
    public class ActivityListViewModel
    {
        public PaginatedList<Activity> Activities { get; set; } = null!;
        public string? SearchTerm { get; set; }
        public string? TypeFilter { get; set; }
        public string? StatusFilter { get; set; }
        public string? AssignedToFilter { get; set; }
        public int PageIndex { get; set; } = 1;

        public SelectList? UsersList { get; set; }
    }

    public class ActivityFormViewModel
    {
        public int ActivityId { get; set; }

        [Required(ErrorMessage = "Activity Type is required.")]
        [Display(Name = "Activity Type")]
        public string ActivityType { get; set; } = "Call"; // Call, Meeting, Email, Task

        [Required(ErrorMessage = "Subject is required.")]
        [StringLength(150, ErrorMessage = "Subject cannot exceed 150 characters.")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required.")]
        [StringLength(1500)]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Activity Date is required.")]
        [Display(Name = "Activity Date")]
        public DateTime ActivityDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Related Customer (Optional)")]
        public int? CustomerId { get; set; }

        [Display(Name = "Related Lead (Optional)")]
        public int? LeadId { get; set; }

        [Required(ErrorMessage = "Assigned user is required.")]
        [Display(Name = "Assigned To")]
        public string AssignedToId { get; set; } = string.Empty;

        [Required]
        public string Status { get; set; } = "Completed"; // Pending, In Progress, Completed, Cancelled

        public SelectList? CustomersList { get; set; }
        public SelectList? LeadsList { get; set; }
        public SelectList? UsersList { get; set; }
    }
}
