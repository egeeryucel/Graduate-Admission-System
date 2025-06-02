using GraduationAdmissionSystem.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GraduationAdmissionSystem.ViewModels
{
    public class DepartmentChairApplicationDetailsViewModel
    {
        [Required]
        public int ApplicationId { get; set; }

        public string? ApplicantFullName { get; set; }
        public string? ApplicantEmail { get; set; }
        public string? ApplicationStatus { get; set; }
        public string? ApplicationCreatedAt { get; set; }
        public string? ApplicationForwardedAt { get; set; }
        public string? ApplicationPeriodName { get; set; }
        public ICollection<ProgramChoice> ProgramChoices { get; set; } = new List<ProgramChoice>();

        [Display(Name = "Department Chair/Interview Notes")]
        public string? DepartmentChairNotes { get; set; }

        [Display(Name = "Scholarship Option")]
        [Required(ErrorMessage = "Please select a scholarship option.")]
        public int SelectedScholarshipQuotaId { get; set; }
        public IEnumerable<SelectListItem> AvailableScholarshipQuotas { get; set; } = new List<SelectListItem>();

        [Display(Name = "Notes for Acceptance (Optional)")]
        public string? FinalDecisionNotes { get; set; }

        [Display(Name = "Scientific Preparation Required?")]
        public bool? RequiresScientificPreparation { get; set; } 

        public DepartmentChairApplicationDetailsViewModel()
        {
            AvailableScholarshipQuotas = new List<SelectListItem>();
            ProgramChoices = new List<ProgramChoice>();
        }
    }
} 