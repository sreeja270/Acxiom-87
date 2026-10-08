using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models;
using AcxiomCRM.ViewModels.Common;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.ViewModels
{
    public class FollowUpListViewModel
    {
        public PaginatedList<FollowUp> FollowUps { get; set; } = null!;
        public string? SearchTerm { get; set; }
        public string? StatusFilter { get; set; }
        public string? TypeFilter { get; set; }
        public string? DateFilter { get; set; } // All, Today, Upcoming, Overdue
        public string? AssignedToFilter { get; set; }
        public int PageIndex { get; set; } = 1;

        public int OverdueCount { get; set; }
        public int TodayCount { get; set; }
        public int UpcomingCount { get; set; }

        public SelectList? UsersList { get; set; }
    }

    public class FollowUpFormViewModel : IValidatableObject
    {
        public int FollowUpId { get; set; }

        [Display(Name = "Related Customer (Optional)")]
        public int? CustomerId { get; set; }

        [Display(Name = "Related Lead (Optional)")]
        public int? LeadId { get; set; }

        [Required(ErrorMessage = "Follow-up Date is required.")]
        [Display(Name = "Follow-up Date & Time")]
        public DateTime FollowUpDate { get; set; } = DateTime.Today.AddDays(1).AddHours(10);

        [Required(ErrorMessage = "Follow-up Type is required.")]
        [Display(Name = "Activity Type")]
        public string FollowUpType { get; set; } = "Call"; // Call, Meeting, Email, Demo, Check-in

        [Required(ErrorMessage = "Remarks are required.")]
        [StringLength(1000)]
        [Display(Name = "Action Remarks / Goal")]
        public string Remarks { get; set; } = string.Empty;

        [Required]
        public string Status { get; set; } = "Planned"; // Planned, Completed, Missed, Cancelled

        [Required(ErrorMessage = "Assigned Sales Executive is required.")]
        [Display(Name = "Assigned To")]
        public string AssignedToId { get; set; } = string.Empty;

        [StringLength(1000)]
        [Display(Name = "Completion Notes")]
        public string? CompletionNotes { get; set; }

        public SelectList? CustomersList { get; set; }
        public SelectList? LeadsList { get; set; }
        public SelectList? UsersList { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Business rule: New / Planned follow-up date cannot be earlier than today
            if (Status == "Planned" && FollowUpId == 0 && FollowUpDate.Date < DateTime.Today)
            {
                yield return new ValidationResult(
                    "Follow-up date cannot be earlier than today.",
                    new[] { nameof(FollowUpDate) });
            }
        }
    }

    public class FollowUpStatusChangeViewModel
    {
        public int FollowUpId { get; set; }
        public string Status { get; set; } = "Completed";
        public string? CompletionNotes { get; set; }
        public DateTime? NewFollowUpDate { get; set; }
    }
}
