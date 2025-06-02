using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GraduationAdmissionSystem.ViewModels
{
    public class EditUserViewModel
    {
        public string Id { get; set; }

        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Display(Name = "Current Role")]
        public string? CurrentRole { get; set; } 
        
        [Display(Name = "New Role")]
        public string? NewRoleAssignment { get; set; } 

        [Display(Name = "Managed Departments")]
        public List<int>? SelectedDepartmentIds { get; set; } = new List<int>();

        public SelectList? AllRoles { get; set; }
        public List<SelectListItem>? AllDepartments { get; set; } = new List<SelectListItem>();
    }
} 