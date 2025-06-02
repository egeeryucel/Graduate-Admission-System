using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GraduationAdmissionSystem.Models
{
    public class Program
    {
        [Key]
        public int ProgramId { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        [StringLength(200)]
        public string? NameEn { get; set; }

        [Required]
        [StringLength(50)]
        public string Language { get; set; }

        [Required]
        [StringLength(200)]
        public string FacultyInstitute { get; set; } 

        [Required]
        [StringLength(200)]
        public string FacultyInstituteEn { get; set; }

        [Required]
        [StringLength(50)]
        public string Level { get; set; }

        public int? DepartmentId { get; set; }

        [ForeignKey("DepartmentId")]
        public virtual Department? Department { get; set; }
    }
} 