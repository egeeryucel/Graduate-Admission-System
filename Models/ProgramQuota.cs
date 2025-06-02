using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace GraduationAdmissionSystem.Models
{
    public class ProgramQuota
    {
        public int ProgramQuotaId { get; set; }

        [Required]
        public virtual int ProgramId { get; set; } 
        [ValidateNever]
        public virtual Program Program { get; set; }

        [Required]
        public virtual int ApplicationPeriodId { get; set; } 
        [ValidateNever]
        public virtual ApplicationPeriod ApplicationPeriod { get; set; }

        [Required]
        [Display(Name = "Total Quota")]
        public virtual int TotalQuota { get; set; }

        [ValidateNever]
        public virtual ICollection<ScholarshipQuota> ScholarshipQuotas { get; set; } = new List<ScholarshipQuota>();
    }
} 