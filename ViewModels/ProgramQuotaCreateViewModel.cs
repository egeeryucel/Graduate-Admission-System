using GraduationAdmissionSystem.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GraduationAdmissionSystem.ViewModels
{
    public class ProgramQuotaCreateViewModel : ProgramQuota
    {
        [Display(Name = "Application Period")]
        [Required(ErrorMessage = "Please select an application period.")]
        public override int ApplicationPeriodId { get; set; }

        [Display(Name = "Language of Instruction")]
        [Required(ErrorMessage = "Please select the language of instruction.")]
        public string SelectedLanguage { get; set; }
        public IEnumerable<SelectListItem> LanguageSelectList { get; set; } = new List<SelectListItem>();

        [Display(Name = "Program Level")]
        [Required(ErrorMessage = "Please select the program level.")]
        public string SelectedLevel { get; set; }
        public IEnumerable<SelectListItem> LevelSelectList { get; set; } = new List<SelectListItem>();

        [Display(Name = "Program")]
        [Required(ErrorMessage = "Please select a program.")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid program.")]
        public override int ProgramId { get; set; }

        [Required(ErrorMessage = "Please enter the total quota.")]
        [Range(0, int.MaxValue, ErrorMessage = "Total quota cannot be negative.")]
        [Display(Name = "Total Quota")]
        public override int TotalQuota { get; set; }

        public IEnumerable<SelectListItem> ApplicationPeriodSelectList { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> ProgramSelectList { get; set; } = new List<SelectListItem>();
    }
} 