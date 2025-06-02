using GraduationAdmissionSystem.Models; 
using System;
using System.Collections.Generic;

namespace GraduationAdmissionSystem.ViewModels
{
    public class StudentTemporaryDocument
    {
        public Guid TempDocId { get; set; }
        public string DocumentType { get; set; }
        public string FileName { get; set; }
        public string TemporaryFilePath { get; set; }
        public string ContentType { get; set; }
        public long FileSize { get; set; }

        public StudentTemporaryDocument()
        {
            TempDocId = Guid.NewGuid();
        }
    }

    public class StudentApplicationItem
    {
        public Guid TemporaryId { get; set; } 
        public string StudentName { get; set; } 

        public ProgramSelection ProgramSelectionData { get; set; }
        public PersonalInformation PersonalInformationData { get; set; }
        public EducationalInformation EducationalInformationData { get; set; }
        public List<StudentTemporaryDocument> TemporaryDocuments { get; set; }

        public int CurrentStep { get; set; }

        public StudentApplicationItem()
        {
            TemporaryId = Guid.NewGuid();

            ProgramSelectionData = new ProgramSelection();
            PersonalInformationData = new PersonalInformation();
            EducationalInformationData = new EducationalInformation();
            TemporaryDocuments = new List<StudentTemporaryDocument>();
            CurrentStep = 1; 
            StudentName = "New Student"; 
        }
    }
} 