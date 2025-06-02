using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GraduationAdmissionSystem.ViewModels
{
    public class DocumentsViewModel
    {
        public int ProgramSelectionId { get; set; }

        [Display(Name = "Passport Copy")]
        public IFormFile? PassportCopy { get; set; }

        [Display(Name = "High School Diploma")]
        public IFormFile? HighSchoolDiploma { get; set; }

        [Display(Name = "Transcript")]
        public IFormFile? Transcript { get; set; }

        [Display(Name = "CV/Resume")]
        public IFormFile? CV { get; set; }

        [Display(Name = "Blue Card Copy")]
        public IFormFile? BlueCardCopy { get; set; }

        [Display(Name = "Language Proficiency Certificate")]
        public IFormFile? LanguageCertificate { get; set; }

        [Display(Name = "Additional Documents")]
        public List<IFormFile> AdditionalDocuments { get; set; } = new List<IFormFile>();
    }
} 