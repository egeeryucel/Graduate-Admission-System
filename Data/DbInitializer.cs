using Microsoft.AspNetCore.Identity;
using GraduationAdmissionSystem.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq; 
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging; 
using Microsoft.AspNetCore.Builder; 

using DataProgram = GraduationAdmissionSystem.Models.Program;

namespace GraduationAdmissionSystem.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(IApplicationBuilder app)
        {
            using (var scope = app.ApplicationServices.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<ApplicationDbContext>();
                    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
                    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
                    await context.Database.MigrateAsync();

                    string[] roleNames = { "Admin", "Secretary", "DepartmentChair", "Candidate", "Agency" };
                    foreach (var roleName in roleNames)
                    {
                        var roleExist = await roleManager.RoleExistsAsync(roleName);
                        if (!roleExist)
                        {
                            await roleManager.CreateAsync(new IdentityRole(roleName));
                        }
                    }

                    if (await userManager.FindByEmailAsync("admin@admin.com") == null)
                    {
                        ApplicationUser user = new ApplicationUser
                        {
                            UserName = "admin@admin.com",
                            Email = "admin@admin.com",
                            FirstName = "Admin",
                            LastName = "User",
                            Role = "Admin",
                            IsApproved = true, 
                            RegistrationDate = DateTime.UtcNow,
                            EmailConfirmed = true 
                        };

                        var result = await userManager.CreateAsync(user, "Admin123!");
                        if (result.Succeeded)
                        {
                            await userManager.AddToRoleAsync(user, "Admin");
                        }
                    }
                    if (!context.Departments.Any())
                    {
                        var departments = new List<Department>
                        {
                            new Department { Name = "Computer Engineering" },
                            new Department { Name = "Electrical and Electronics Engineering" },
                            new Department { Name = "Industrial Engineering" },
                            new Department { Name = "Information Technologies" },
                            new Department { Name = "Civil Engineering" },
                            new Department { Name = "Mechanical Engineering" },
                            new Department { Name = "Mathematics" },
                            new Department { Name = "Business Administration" },
                            new Department { Name = "Economics" },
                            new Department { Name = "Psychology" },
                            new Department { Name = "Interior Architecture" },
                            new Department { Name = "Art Theory and Criticism" },
                            new Department { Name = "Painting" },
                            new Department { Name = "Visual Communication Design" },
                            new Department { Name = "Fashion and Textile Design" },
                            new Department { Name = "Cinema and Television" },
                            new Department { Name = "Middle Eastern Studies" },
                            new Department { Name = "Management Information Systems" },
                            new Department { Name = "Finance" },
                            new Department { Name = "Occupational Health and Safety" },
                            new Department { Name = "Art Science" },
                            new Department { Name = "Cyber Security" },
                            new Department { Name = "Landscape Architecture" },
                            new Department { Name = "Contemporary Business Management" },
                        };
                        context.Departments.AddRange(departments);
                        await context.SaveChangesAsync();
                    }
                    if (!context.Programs.Any())
                    {
                        var depts = context.Departments.ToDictionary(d => d.Name, d => d.DepartmentId);

                        var programs = new List<DataProgram>
                        {
                            new DataProgram { Name = "Yöneticiler İçin MBA", NameEn = "MBA for Executives", Language = "English", FacultyInstitute = "İşletme Enstitüsü", FacultyInstituteEn = "Graduate School of Business", Level = "MasterThesis", DepartmentId = depts["Business Administration"] },
                            new DataProgram { Name = "Bilgisayar Mühendisliği Yüksek Lisans Programı", NameEn = "Computer Engineering Master\'s Program", Language = "English", FacultyInstitute = "Mühendislik Fakültesi", FacultyInstituteEn = "Faculty of Engineering", Level = "MasterThesis", DepartmentId = depts["Computer Engineering"] },
                            new DataProgram { Name = "Yöneticiler İçin MBA", NameEn = "MBA for Executives", Language = "Turkish", FacultyInstitute = "İşletme Enstitüsü", FacultyInstituteEn = "Graduate School of Business", Level = "MasterThesis", DepartmentId = depts["Business Administration"] },
                            new DataProgram { Name = "Elektronik Mühendisliği Yüksek Lisans Programı", NameEn = "Electronic Engineering Master\'s Program", Language = "English", FacultyInstitute = "Mühendislik Fakültesi", FacultyInstituteEn = "Faculty of Engineering", Level = "MasterThesis", DepartmentId = depts["Electrical and Electronics Engineering"] },
                            new DataProgram { Name = "Muhasebe ve Denetim", NameEn = "Accounting and Auditing", Language = "Turkish", FacultyInstitute = "Sosyal Bilimler Enstitüsü", FacultyInstituteEn = "Graduate School of Social Sciences", Level = "MasterThesis", DepartmentId = depts["Business Administration"] },
                            new DataProgram { Name = "Endüstri Mühendisliği - Yönetim Araştırması", NameEn = "Industrial Engineering - Management Research", Language = "English", FacultyInstitute = "Mühendislik Fakültesi", FacultyInstituteEn = "Faculty of Engineering", Level = "MasterThesis", DepartmentId = depts["Industrial Engineering"] },
                            new DataProgram { Name = "Uygulamalı Ekonomi", NameEn = "Applied Economics", Language = "English", FacultyInstitute = "Sosyal Bilimler Enstitüsü", FacultyInstituteEn = "Graduate School of Social Sciences", Level = "MasterThesis", DepartmentId = depts["Economics"] },
                            new DataProgram { Name = "Enformasyon Teknolojileri Yüksek Lisans", NameEn = "Information Technologies Master\'s Program", Language = "English", FacultyInstitute = "Mühendislik Fakültesi", FacultyInstituteEn = "Faculty of Engineering", Level = "MasterThesis", DepartmentId = depts["Information Technologies"] },
                            new DataProgram { Name = "Uluslararası İlişkiler", NameEn = "International Relations", Language = "English", FacultyInstitute = "Sosyal Bilimler Enstitüsü", FacultyInstituteEn = "Graduate School of Social Sciences", Level = "MasterThesis", DepartmentId = depts["Business Administration"] }, 
                            new DataProgram { Name = "İnşaat Mühendisliği", NameEn = "Civil Engineering", Language = "English", FacultyInstitute = "Mühendislik Fakültesi", FacultyInstituteEn = "Faculty of Engineering", Level = "MasterThesis", DepartmentId = depts["Civil Engineering"] },
                            new DataProgram { Name = "Klinik Psikoloji", NameEn = "Clinical Psychology", Language = "Turkish", FacultyInstitute = "Sosyal Bilimler Enstitüsü", FacultyInstituteEn = "Graduate School of Social Sciences", Level = "MasterThesis", DepartmentId = depts["Psychology"] },
                            new DataProgram { Name = "Makine Mühendisliği Yüksek Lisans Programı", NameEn = "Mechanical Engineering Master\'s Program", Language = "English", FacultyInstitute = "Mühendislik Fakültesi", FacultyInstituteEn = "Faculty of Engineering", Level = "MasterThesis", DepartmentId = depts["Mechanical Engineering"] },
                            new DataProgram { Name = "İç Mimarlık", NameEn = "Interior Architecture", Language = "Turkish", FacultyInstitute = "Mimarlık Fakültesi", FacultyInstituteEn = "Faculty of Architecture", Level = "MasterThesis", DepartmentId = depts["Interior Architecture"] },
                            new DataProgram { Name = "Matematik Yüksek Lisans", NameEn = "Mathematics Master\'s Program", Language = "English", FacultyInstitute = "Fen Bilimleri Enstitüsü", FacultyInstituteEn = "Graduate School of Natural and Applied Sciences", Level = "MasterThesis", DepartmentId = depts["Mathematics"] },
                            new DataProgram { Name = "Sanat Kuramı ve Eleştiri", NameEn = "Art Theory and Criticism", Language = "Turkish", FacultyInstitute = "Sanat ve Tasarım Fakültesi", FacultyInstituteEn = "Faculty of Art and Design", Level = "MasterThesis", DepartmentId = depts["Art Theory and Criticism"] },
                            new DataProgram { Name = "Peyzaj Mimarlığı", NameEn = "Landscape Architecture", Language = "Turkish", FacultyInstitute = "Mimarlık Fakültesi", FacultyInstituteEn = "Faculty of Architecture", Level = "MasterThesis", DepartmentId = depts["Landscape Architecture"] },
                            new DataProgram { Name = "Resim", NameEn = "Painting", Language = "Turkish", FacultyInstitute = "Sanat ve Tasarım Fakültesi", FacultyInstituteEn = "Faculty of Art and Design", Level = "MasterThesis", DepartmentId = depts["Painting"] },
                            new DataProgram { Name = "Siber Güvenlik Yüksek Lisans Programı", NameEn = "Cyber Security Master\'s Program", Language = "Turkish", FacultyInstitute = "Mühendislik Fakültesi", FacultyInstituteEn = "Faculty of Engineering", Level = "MasterThesis", DepartmentId = depts["Cyber Security"] },
                            new DataProgram { Name = "Görsel İletişim Tasarımı", NameEn = "Visual Communication Design", Language = "Turkish", FacultyInstitute = "Sanat ve Tasarım Fakültesi", FacultyInstituteEn = "Faculty of Art and Design", Level = "MasterThesis", DepartmentId = depts["Visual Communication Design"] },
                            new DataProgram { Name = "Moda ve Tekstil Tasarımı", NameEn = "Fashion and Textile Design", Language = "Turkish", FacultyInstitute = "Sanat ve Tasarım Fakültesi", FacultyInstituteEn = "Faculty of Art and Design", Level = "MasterThesis", DepartmentId = depts["Fashion and Textile Design"] },
                            new DataProgram { Name = "Sinema ve Televizyon", NameEn = "Cinema and Television", Language = "Turkish", FacultyInstitute = "İletişim Fakültesi", FacultyInstituteEn = "Faculty of Communication", Level = "MasterThesis", DepartmentId = depts["Cinema and Television"] },
                            new DataProgram { Name = "Çağdaş İşletme Yönetimi", NameEn = "Contemporary Business Management", Language = "English", FacultyInstitute = "İşletme Enstitüsü", FacultyInstituteEn = "Graduate School of Business", Level = "PhD", DepartmentId = depts["Contemporary Business Management"] },
                            new DataProgram { Name = "Bilgisayar Mühendisliği", NameEn = "Computer Engineering", Language = "English", FacultyInstitute = "Mühendislik Fakültesi", FacultyInstituteEn = "Faculty of Engineering", Level = "PhD", DepartmentId = depts["Computer Engineering"] },
                            new DataProgram { Name = "Sanat Bilimi", NameEn = "Art Science", Language = "Turkish", FacultyInstitute = "Sanat ve Tasarım Fakültesi", FacultyInstituteEn = "Faculty of Art and Design", Level = "PhD", DepartmentId = depts["Art Science"] },
                            new DataProgram { Name = "Elektronik Mühendisliği", NameEn = "Electronic Engineering", Language = "English", FacultyInstitute = "Mühendislik Fakültesi", FacultyInstituteEn = "Faculty of Engineering", Level = "PhD", DepartmentId = depts["Electrical and Electronics Engineering"] },
                            new DataProgram { Name = "Klinik Psikoloji", NameEn = "Clinical Psychology", Language = "Turkish", FacultyInstitute = "Sosyal Bilimler Enstitüsü", FacultyInstituteEn = "Graduate School of Social Sciences", Level = "PhD", DepartmentId = depts["Psychology"] },
                            new DataProgram { Name = "Matematik", NameEn = "Mathematics", Language = "English", FacultyInstitute = "Fen Bilimleri Enstitüsü", FacultyInstituteEn = "Graduate School of Natural and Applied Sciences", Level = "PhD", DepartmentId = depts["Mathematics"] },
                            new DataProgram { Name = "Yöneticiler İçin MBA", NameEn = "MBA for Executives", Language = "English", FacultyInstitute = "İşletme Enstitüsü", FacultyInstituteEn = "Graduate School of Business", Level = "MasterNonThesis", DepartmentId = depts["Business Administration"] },
                            new DataProgram { Name = "Görsel İletişim Tasarımı", NameEn = "Visual Communication Design", Language = "Turkish", FacultyInstitute = "Sanat ve Tasarım Fakültesi", FacultyInstituteEn = "Faculty of Art and Design", Level = "MasterNonThesis", DepartmentId = depts["Visual Communication Design"] },
                            new DataProgram { Name = "Yöneticiler İçin MBA", NameEn = "MBA for Executives", Language = "Turkish", FacultyInstitute = "İşletme Enstitüsü", FacultyInstituteEn = "Graduate School of Business", Level = "MasterNonThesis", DepartmentId = depts["Business Administration"] },
                            new DataProgram { Name = "Moda ve Tekstil Tasarımı", NameEn = "Fashion and Textile Design", Language = "Turkish", FacultyInstitute = "Sanat ve Tasarım Fakültesi", FacultyInstituteEn = "Faculty of Art and Design", Level = "MasterNonThesis", DepartmentId = depts["Fashion and Textile Design"] },
                            new DataProgram { Name = "Muhasebe ve Denetim", NameEn = "Accounting and Auditing", Language = "Turkish", FacultyInstitute = "Sosyal Bilimler Enstitüsü", FacultyInstituteEn = "Graduate School of Social Sciences", Level = "MasterNonThesis", DepartmentId = depts["Business Administration"] },
                            new DataProgram { Name = "Sinema ve Televizyon", NameEn = "Cinema and Television", Language = "Turkish", FacultyInstitute = "İletişim Fakültesi", FacultyInstituteEn = "Faculty of Communication", Level = "MasterNonThesis", DepartmentId = depts["Cinema and Television"] },
                            new DataProgram { Name = "Uygulamalı Ekonomi", NameEn = "Applied Economics", Language = "English", FacultyInstitute = "Sosyal Bilimler Enstitüsü", FacultyInstituteEn = "Graduate School of Social Sciences", Level = "MasterNonThesis", DepartmentId = depts["Economics"] },
                            new DataProgram { Name = "Bilgisayar Mühendisliği Yüksek Lisans Programı", NameEn = "Computer Engineering Master\'s Program", Language = "English", FacultyInstitute = "Mühendislik Fakültesi", FacultyInstituteEn = "Faculty of Engineering", Level = "MasterNonThesis", DepartmentId = depts["Computer Engineering"] },
                            new DataProgram { Name = "Uluslararası İlişkiler", NameEn = "International Relations", Language = "English", FacultyInstitute = "Sosyal Bilimler Enstitüsü", FacultyInstituteEn = "Graduate School of Social Sciences", Level = "MasterNonThesis", DepartmentId = depts["Business Administration"] },
                            new DataProgram { Name = "Elektronik Mühendisliği Yüksek Lisans Programı", NameEn = "Electronic Engineering Master\'s Program", Language = "English", FacultyInstitute = "Mühendislik Fakültesi", FacultyInstituteEn = "Faculty of Engineering", Level = "MasterNonThesis", DepartmentId = depts["Electrical and Electronics Engineering"] },
                            new DataProgram { Name = "Orta Doğu Çalışmaları", NameEn = "Middle Eastern Studies", Language = "English", FacultyInstitute = "Sosyal Bilimler Enstitüsü", FacultyInstituteEn = "Graduate School of Social Sciences", Level = "MasterNonThesis", DepartmentId = depts["Middle Eastern Studies"] },
                            new DataProgram { Name = "Endüstri Mühendisliği - Yönetim Araştırması", NameEn = "Industrial Engineering - Management Research", Language = "English", FacultyInstitute = "Mühendislik Fakültesi", FacultyInstituteEn = "Faculty of Engineering", Level = "MasterNonThesis", DepartmentId = depts["Industrial Engineering"] },
                            new DataProgram { Name = "Klinik Psikoloji", NameEn = "Clinical Psychology", Language = "Turkish", FacultyInstitute = "Sosyal Bilimler Enstitüsü", FacultyInstituteEn = "Graduate School of Social Sciences", Level = "MasterNonThesis", DepartmentId = depts["Psychology"] },
                            new DataProgram { Name = "Enformasyon Teknolojileri Yüksek Lisans", NameEn = "Information Technologies Master\'s Program", Language = "English", FacultyInstitute = "Mühendislik Fakültesi", FacultyInstituteEn = "Faculty of Engineering", Level = "MasterNonThesis", DepartmentId = depts["Information Technologies"] },
                            new DataProgram { Name = "Yönetim Bilişim Sistemleri", NameEn = "Management Information Systems", Language = "English", FacultyInstitute = "Sosyal Bilimler Enstitüsü", FacultyInstituteEn = "Graduate School of Social Sciences", Level = "MasterNonThesis", DepartmentId = depts["Management Information Systems"] },
                            new DataProgram { Name = "Finans Mühendisliği", NameEn = "Financial Engineering", Language = "English", FacultyInstitute = "Fen Bilimleri Enstitüsü", FacultyInstituteEn = "Graduate School of Natural and Applied Sciences", Level = "MasterNonThesis", DepartmentId = depts["Finance"] },
                            new DataProgram { Name = "İç Mimarlık", NameEn = "Interior Architecture", Language = "Turkish", FacultyInstitute = "Mimarlık Fakültesi", FacultyInstituteEn = "Faculty of Architecture", Level = "MasterNonThesis", DepartmentId = depts["Interior Architecture"] },
                            new DataProgram { Name = "İnşaat Mühendisliği", NameEn = "Civil Engineering", Language = "English", FacultyInstitute = "Mühendislik Fakültesi", FacultyInstituteEn = "Faculty of Engineering", Level = "MasterNonThesis", DepartmentId = depts["Civil Engineering"] },
                            new DataProgram { Name = "Sanat Kuramı ve Eleştiri", NameEn = "Art Theory and Criticism", Language = "Turkish", FacultyInstitute = "Sanat ve Tasarım Fakültesi", FacultyInstituteEn = "Faculty of Art and Design", Level = "MasterNonThesis", DepartmentId = depts["Art Theory and Criticism"] },
                            new DataProgram { Name = "Makine Mühendisliği Yüksek Lisans Programı", NameEn = "Mechanical Engineering Master\'s Program", Language = "English", FacultyInstitute = "Mühendislik Fakültesi", FacultyInstituteEn = "Faculty of Engineering", Level = "MasterNonThesis", DepartmentId = depts["Mechanical Engineering"] },
                            new DataProgram { Name = "Resim", NameEn = "Painting", Language = "Turkish", FacultyInstitute = "Sanat ve Tasarım Fakültesi", FacultyInstituteEn = "Faculty of Art and Design", Level = "MasterNonThesis", DepartmentId = depts["Painting"] },
                            new DataProgram { Name = "İş Sağlığı ve Güvenliği", NameEn = "Occupational Health and Safety", Language = "Turkish", FacultyInstitute = "Fen Bilimleri Enstitüsü", FacultyInstituteEn = "Graduate School of Natural and Applied Sciences", Level = "MasterNonThesis", DepartmentId = depts["Occupational Health and Safety"] },
                        };
                        var existingProgramNames = context.Programs.Select(p => p.Name + "|" + p.Language + "|" + p.Level).ToHashSet();
                        var programsToAdd = programs.Where(p => !existingProgramNames.Contains(p.Name + "|" + p.Language + "|" + p.Level)).ToList();

                        context.Programs.AddRange(programsToAdd);
                        await context.SaveChangesAsync();
                    }

                    var departmentChairsRole = await roleManager.FindByNameAsync("DepartmentChair");
                    if (departmentChairsRole == null)
                    {
                        departmentChairsRole = new IdentityRole("DepartmentChair");
                        await roleManager.CreateAsync(departmentChairsRole);
                    }
                    var compEng = context.Departments.FirstOrDefault(d => d.Name == "Computer Engineering");
                    var softEng = context.Departments.FirstOrDefault(d => d.Name == "Software Engineering");
                    var arch = context.Departments.FirstOrDefault(d => d.Name == "Architecture");
                    var healthServices = context.Departments.FirstOrDefault(d => d.Name == "Vocational School of Health Services");


                    if (compEng != null && softEng != null)
                    {
                        var chairUser = await userManager.FindByEmailAsync("ahmet.feyzi.ates@example.com");
                        if (chairUser == null)
                        {
                            chairUser = new ApplicationUser
                            {
                                UserName = "ahmet.feyzi.ates@example.com",
                                Email = "ahmet.feyzi.ates@example.com",
                                FirstName = "Ahmet Feyzi",
                                LastName = "Ateş",
                                EmailConfirmed = true,
                                ManagedDepartments = new List<Department>()
                            };
                            await userManager.CreateAsync(chairUser, "DefaultPassword123!");
                            await userManager.AddToRoleAsync(chairUser, "DepartmentChair");
                        }
                        if (chairUser.ManagedDepartments == null)
                        {
                            chairUser.ManagedDepartments = new List<Department>();
                        }

                        if (!chairUser.ManagedDepartments.Any(d => d.DepartmentId == compEng.DepartmentId))
                        {
                            chairUser.ManagedDepartments.Add(compEng);
                        }
                        if (!chairUser.ManagedDepartments.Any(d => d.DepartmentId == softEng.DepartmentId))
                        {
                            chairUser.ManagedDepartments.Add(softEng);
                        }
                        await context.SaveChangesAsync(); 
                    }
                    else
                    {
                        
                    }
                }
                catch (Exception ex)
                {
                    var loggerFactory = services.GetRequiredService<ILoggerFactory>();
                    var logger = loggerFactory.CreateLogger<Program>();
                    logger.LogError(ex, "An error occurred while seeding the database.");

                }
            }
        }
    }
} 