using System.ComponentModel.DataAnnotations;

namespace GraduationAdmissionSystem.Models
{
    public class PersonalInformation
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "First Name is required")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last Name is required")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Display(Name = "Passport Number")]
        public string? PassportNumber { get; set; }

        [Required(ErrorMessage = "Gender is required")]
        public string Gender { get; set; }

        [Required(ErrorMessage = "Date of Birth is required")]
        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "Country of Birth is required")]
        [Display(Name = "Country of Birth")]
        public string CountryOfBirth { get; set; }

        [Required(ErrorMessage = "City of Birth is required")]
        [Display(Name = "City of Birth")]
        public string CityOfBirth { get; set; }

        [Display(Name = "Dual Citizenship")]
        public bool HasDualCitizenship { get; set; }

        [Required(ErrorMessage = "Citizenship is required")]
        public string Citizenship { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [Phone]
        [Display(Name = "Phone")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        [Display(Name = "Email Address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Address is required")]
        public string Address { get; set; }

        [Required(ErrorMessage = "Country of Residence is required")]
        [Display(Name = "Country of Residence")]
        public string CountryOfResidence { get; set; }

        [Display(Name = "City of Residence")]
        public string? CityOfResidence { get; set; }

        [Required(ErrorMessage = "Father's Name is required")]
        [Display(Name = "Father's Name")]
        public string FatherName { get; set; }

        [Required(ErrorMessage = "Mother's Name is required")]
        [Display(Name = "Mother's Name")]
        public string MotherName { get; set; }

        public int ProgramSelectionId { get; set; }
        public ProgramSelection ProgramSelection { get; set; }
    }
} 