using System.ComponentModel.DataAnnotations;

namespace GraduationAdmissionSystem.Models
{
    public class ProgramApplication
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Language { get; set; }

        [Required]
        public string FacultyInstitute { get; set; }

        [Required]
        public string ProgramName { get; set; }

        public string? ApplicationStatus { get; set; }

        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
} 