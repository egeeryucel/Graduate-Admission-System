using System.ComponentModel.DataAnnotations;

namespace GraduationAdmissionSystem.Models
{
    public class Department
    {
        [Key]
        public int DepartmentId { get; set; }

        [Required]
        [StringLength(100)]
        public required string Name { get; set; }
        public virtual ICollection<ApplicationUser> DepartmentChairs { get; set; } = new List<ApplicationUser>();
    }
} 