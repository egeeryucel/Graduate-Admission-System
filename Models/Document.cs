using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GraduationAdmissionSystem.Models
{
    public class Document
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Document Type")]
        public string DocumentType { get; set; }

        [Required]
        [Display(Name = "File Name")]
        public string FileName { get; set; }

        [Display(Name = "Original File Name")]
        public string OriginalFileName { get; set; }

        [Display(Name = "Content Type")]
        public string ContentType { get; set; }

        [Display(Name = "File Size (bytes)")]
        public long FileSize { get; set; }

        [Required]
        public byte[] FileContent { get; set; }

        [Required]
        public DateTime UploadDate { get; set; }

        public int ProgramSelectionId { get; set; }
        
        [ForeignKey("ProgramSelectionId")]
        public ProgramSelection ProgramSelection { get; set; }
    }
} 