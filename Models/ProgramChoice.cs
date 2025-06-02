using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GraduationAdmissionSystem.Models
{
    public class ProgramChoice
    {
        [Key]
        public int Id { get; set; }

        public int ProgramSelectionId { get; set; }
        public virtual ProgramSelection ProgramSelection { get; set; }

        [Required]
        public required string ProgramName { get; set; }

        [Required]
        public int Choice { get; set; }
        public int? DepartmentId { get; set; } 

        [ForeignKey("DepartmentId")]
        public virtual Department? Department { get; set; }
    }
} 