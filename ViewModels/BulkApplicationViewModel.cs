using System.Collections.Generic;
using System;

namespace GraduationAdmissionSystem.ViewModels
{
    public class BulkApplicationViewModel
    {
        public List<StudentApplicationItem> StudentApplications { get; set; }
        public Guid? CurrentEditingStudentTemporaryId { get; set; }

        public BulkApplicationViewModel()
        {
            StudentApplications = new List<StudentApplicationItem>();
        }
    }
} 