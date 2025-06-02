using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using GraduationAdmissionSystem.Models;

namespace GraduationAdmissionSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<ProgramSelection> ProgramSelections { get; set; }
        public DbSet<ProgramChoice> ProgramChoices { get; set; }
        public DbSet<PersonalInformation> PersonalInformations { get; set; }
        public DbSet<EducationalInformation> EducationalInformations { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<GraduationAdmissionSystem.Models.Program> Programs { get; set; }
        public DbSet<ApplicationPeriod> ApplicationPeriods { get; set; }
        public DbSet<ProgramQuota> ProgramQuotas { get; set; }
        public DbSet<ScholarshipQuota> ScholarshipQuotas { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ProgramChoice>()
                .HasOne(pc => pc.ProgramSelection)
                .WithMany(ps => ps.ProgramChoices)
                .HasForeignKey(pc => pc.ProgramSelectionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PersonalInformation>()
                .HasOne(pi => pi.ProgramSelection)
                .WithOne()
                .HasForeignKey<PersonalInformation>(pi => pi.ProgramSelectionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<EducationalInformation>()
                .HasOne(ei => ei.ProgramSelection)
                .WithOne()
                .HasForeignKey<EducationalInformation>(ei => ei.ProgramSelectionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<EducationalInformation>()
                .Property(ei => ei.GPA)
                .HasPrecision(3, 2);

            builder.Entity<EducationalInformation>()
                .Property(ei => ei.LanguageExamScore)
                .HasPrecision(5, 2);
        }
    }
} 