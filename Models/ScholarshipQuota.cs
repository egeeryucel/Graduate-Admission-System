using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace GraduationAdmissionSystem.Models
{
    public class ScholarshipQuota
    {
        public int ScholarshipQuotaId { get; set; }

        [Required]
        public int ProgramQuotaId { get; set; } 
        [ValidateNever]
        public virtual ProgramQuota ProgramQuota { get; set; }

        [Required]
        [Range(0, 100, ErrorMessage = "Scholarship percentage must be between 0 and 100.")]
        [Display(Name = "Scholarship Percentage (%)")]
        public int ScholarshipPercentage { get; set; } 

        [Required]
        [Display(Name = "Allocated Quota")]
        public int AllocatedQuota { get; set; } 
    }
} 