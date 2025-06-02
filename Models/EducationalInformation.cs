using System.ComponentModel.DataAnnotations;

namespace GraduationAdmissionSystem.Models
{
    public class EducationalInformation
    {
        [Key]
        public int Id { get; set; }

        [Display(Name = "Blue Card Owner")]
        public bool IsBlueCardOwner { get; set; }

        [Required(ErrorMessage = "School Name is required")]
        [Display(Name = "School Name")]
        public string SchoolName { get; set; }

        [Required(ErrorMessage = "Graduation Year is required")]
        [Display(Name = "Graduation Year")]
        [Range(1900, 2100, ErrorMessage = "Please enter a valid graduation year")]
        public int GraduationYear { get; set; }

        [Required(ErrorMessage = "The Degree field is required.")]
        [Display(Name = "Degree")]
        public string Degree { get; set; }

        [Required(ErrorMessage = "The Major field is required.")]
        [Display(Name = "Major")]
        public string Major { get; set; }

        [Required(ErrorMessage = "GPA is required")]
        [Display(Name = "GPA")]
        [Range(0, 4.0, ErrorMessage = "GPA must be between 0 and 4.0")]
        public decimal GPA { get; set; }

        [Required(ErrorMessage = "Country is required")]
        [Display(Name = "High School Graduation Country")]
        public string Country { get; set; }

        [Required(ErrorMessage = "City is required")]
        [Display(Name = "City")]
        public string City { get; set; }

        [Required(ErrorMessage = "Language Proficiency is required")]
        [Display(Name = "Language Proficiency")]
        public string LanguageProficiency { get; set; }

        [Required(ErrorMessage = "Language Exam Score is required")]
        [Display(Name = "Language Exam Score")]
        [Range(0, 100, ErrorMessage = "Please enter a valid exam score between 0 and 100")]
        public decimal LanguageExamScore { get; set; }

        public int ProgramSelectionId { get; set; }
        public ProgramSelection ProgramSelection { get; set; }
    }
} 