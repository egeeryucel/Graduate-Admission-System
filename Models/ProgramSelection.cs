using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GraduationAdmissionSystem.Models
{
    public class ProgramSelection
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Language { get; set; }

        [Required]
        public string FacultyInstitute { get; set; }

        [Required]
        public string ProgramLevel { get; set; }

        [Required]
        public string ApplicationStatus { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [Display(Name = "Tracking Number")]
        public string? TrackingNumber { get; set; } = string.Empty;

        public string? UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        public string? SubmittedByAgencyUserId { get; set; }

        public virtual ICollection<ProgramChoice> ProgramChoices { get; set; } = new List<ProgramChoice>();

        public string? AssignedDepartmentChairId { get; set; }
        [ForeignKey("AssignedDepartmentChairId")]
        public virtual ApplicationUser? AssignedDepartmentChair { get; set; }

        public DateTime? ForwardedToDepartmentAt { get; set; }
        public DateTime? DepartmentDecisionDate { get; set; }
        public string? DepartmentChairNotes { get; set; }
        public string? RejectionReason { get; set; }
        public string? ReviewComment { get; set; }

        [Display(Name = "Scholarship Rate (%)")]
        public int? ScholarshipRate { get; set; }

        [Display(Name = "Final Decision Notes")]
        public string? FinalDecisionNotes { get; set; }

        [Display(Name = "Requires Scientific Preparation")]
        public bool? RequiresScientificPreparation { get; set; }

        public int? ApplicationPeriodId { get; set; }
        [ForeignKey("ApplicationPeriodId")]
        public virtual ApplicationPeriod? ApplicationPeriod { get; set; }


        public int? AwardedScholarshipQuotaId { get; set; }
        [ForeignKey("AwardedScholarshipQuotaId")]
        public virtual ScholarshipQuota? AwardedScholarshipQuota { get; set; }

        public virtual PersonalInformation PersonalInformation { get; set; }
        public virtual EducationalInformation EducationalInformation { get; set; }
        public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
    }
} 