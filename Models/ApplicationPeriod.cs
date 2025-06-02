using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GraduationAdmissionSystem.Models
{
    public class ApplicationPeriod
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Period Name")]
        public string Name { get; set; } 

        [Required]
        public Semester Semester { get; set; }

        [Required]
        public int Year { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; }

        [Display(Name = "Is Open for Applications?")]
        public bool IsOpen { get; set; } = false; 

        public virtual ICollection<ProgramSelection> ProgramSelections { get; set; } = new List<ProgramSelection>();
    }
} 