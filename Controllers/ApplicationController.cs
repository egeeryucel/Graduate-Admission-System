using Microsoft.AspNetCore.Mvc;
using GraduationAdmissionSystem.Models;
using GraduationAdmissionSystem.ViewModels;
using GraduationAdmissionSystem.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace GraduationAdmissionSystem.Controllers
{
    [Authorize]
    public class ApplicationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ApplicationController> _logger;

        public ApplicationController(ApplicationDbContext context, ILogger<ApplicationController> logger)
        {
            _context = context;
            _logger = logger;
        }
        public async Task<IActionResult> ProgramSelection()
        {
            var now = DateTime.UtcNow.Date;
            var openPeriod = await _context.ApplicationPeriods
                                        .FirstOrDefaultAsync(p => p.IsOpen && 
                                                                p.StartDate.Date <= now && 
                                                                p.EndDate.Date >= now);

            if (openPeriod == null)
            {
                TempData["Error"] = "The application period is currently closed. You cannot start a new application at this time.";
                return RedirectToAction("Index", "Home", new { area = "Candidate" }); 
            }
            ViewBag.ApplicationPeriodName = openPeriod.Name; 
            var trackingNumber = HttpContext.Session.GetString("TrackingNumber");
            
            if (!string.IsNullOrEmpty(trackingNumber))
            {
                var existingApplications = await _context.ProgramSelections
                    .Where(ps => ps.TrackingNumber == trackingNumber)
                    .ToListAsync();

                ViewBag.ExistingApplications = existingApplications;
                ViewBag.TrackingNumber = trackingNumber;
            }
            else 
            {
                ViewBag.ExistingApplications = new List<ProgramSelection>();
            }
            var programLevels = new List<SelectListItem>
            {
                new SelectListItem { Value = "MasterThesis", Text = "Master with Thesis" },
                new SelectListItem { Value = "MasterNonThesis", Text = "Master without Thesis" },
                new SelectListItem { Value = "PhD", Text = "Doctorate (PhD)" }
            };
            ViewBag.ProgramLevels = programLevels;
            
            var model = new ProgramSelection
            {
                Language = "",
                ApplicationStatus = "Draft"
            };
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProgramSelection(string language, string level, List<string> selectedPrograms)
        {
            var now = DateTime.UtcNow.Date;
            var openPeriod = await _context.ApplicationPeriods
                                        .FirstOrDefaultAsync(p => p.IsOpen && 
                                                                p.StartDate.Date <= now && 
                                                                p.EndDate.Date >= now);

            if (openPeriod == null)
            {
                TempData["Error"] = "The application period is currently closed. You cannot submit a new application.";
                var programLevelsForError = new List<SelectListItem>
                {
                    new SelectListItem { Value = "MasterThesis", Text = "Master with Thesis" },
                    new SelectListItem { Value = "MasterNonThesis", Text = "Master without Thesis" },
                    new SelectListItem { Value = "PhD", Text = "Doctorate (PhD)" }
                };
                ViewBag.ProgramLevels = programLevelsForError;
                ModelState.AddModelError("", "The application period is currently closed.");
                return View(new ProgramSelection { Language = language ?? "", ApplicationStatus = "Draft"});
            }

            if (string.IsNullOrEmpty(language) || string.IsNullOrEmpty(level) || selectedPrograms == null || !selectedPrograms.Any())
            {
                var programLevels = new List<SelectListItem>
                {
                    new SelectListItem { Value = "MasterThesis", Text = "Master with Thesis" },
                    new SelectListItem { Value = "MasterNonThesis", Text = "Master without Thesis" },
                    new SelectListItem { Value = "PhD", Text = "Doctorate (PhD)" }
                };
                ViewBag.ProgramLevels = programLevels;

                ModelState.AddModelError("", "Please select language, program level, and at least one program.");
                return View(new ProgramSelection 
                { 
                    Language = language ?? "",
                    ApplicationStatus = "Draft"
                });
            }
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var selectedProgramDetails = await _context.Programs
                .Include(p => p.Department) 
                .Where(p => selectedPrograms.Contains(p.Name) && p.Language == language && p.Level == level)
                .ToListAsync();

            _logger.LogInformation($"User {userId} - Selected programs for language '{language}', level '{level}':");
            foreach (var progDetail in selectedProgramDetails)
            {
                _logger.LogInformation($"  - Program: {progDetail.Name}, DepartmentId: {progDetail.DepartmentId?.ToString() ?? "NULL"}, DepartmentName: {progDetail.Department?.Name ?? "N/A"}");
            }

            var programDetailsDict = selectedProgramDetails.ToDictionary(p => p.Name);
            if (userId != null && openPeriod != null) 
            {
                foreach (var programDetail in selectedProgramDetails)
                {
                    if (programDetail.DepartmentId.HasValue)
                    {
                        var existingApplication = await _context.ProgramSelections
                            .Include(ps => ps.ProgramChoices)
                            .FirstOrDefaultAsync(ps =>
                                ps.UserId == userId &&
                                ps.ApplicationPeriodId == openPeriod.Id &&
                                ps.ProgramChoices.Any(pc => pc.DepartmentId == programDetail.DepartmentId.Value));

                        if (existingApplication != null)
                        {
                            _logger.LogWarning($"User {userId} already applied to department {programDetail.DepartmentId.Value} in period {openPeriod.Id}. Application ID: {existingApplication.Id}");
                            ModelState.AddModelError("", $"You have already applied to the department '{programDetail.Department?.Name ?? "N/A"}' in the current application period. You cannot apply to the same department multiple times within the same period.");
                            var programLevelsForError = new List<SelectListItem>
                            {
                                new SelectListItem { Value = "MasterThesis", Text = "Master with Thesis" },
                                new SelectListItem { Value = "MasterNonThesis", Text = "Master without Thesis" },
                                new SelectListItem { Value = "PhD", Text = "Doctorate (PhD)" }
                            };
                            ViewBag.ProgramLevels = programLevelsForError;
                            return View(new ProgramSelection { Language = language ?? "", ApplicationStatus = "Draft"});
                        }
                    }
                }
            }
            string firstProgramName = selectedPrograms.FirstOrDefault();
            string facultyInstituteForSelection = null;
            if (!string.IsNullOrEmpty(firstProgramName) && programDetailsDict.TryGetValue(firstProgramName, out var firstProgramDetail))
            {
                facultyInstituteForSelection = firstProgramDetail.FacultyInstitute;
                _logger.LogInformation($"Setting FacultyInstitute for ProgramSelection from first choice \'{firstProgramName}\': {facultyInstituteForSelection}");
            }
            else
            {
                _logger.LogWarning($"Could not find details for the first selected program \'{firstProgramName}\' to set FacultyInstitute. Setting to empty string.");
                facultyInstituteForSelection = string.Empty; 
            }

            var programSelection = new ProgramSelection
            {
                Language = language,
                FacultyInstitute = facultyInstituteForSelection, 
                ProgramLevel = level,
                ApplicationStatus = "Draft",
                CreatedAt = DateTime.UtcNow,
                TrackingNumber = GenerateTrackingNumber(),
                UserId = userId, 
                ApplicationPeriodId = openPeriod.Id 
            };

            for (int i = 0; i < selectedPrograms.Count; i++)
            {
                var selectedProgramName = selectedPrograms[i];
                int? departmentId = null;

                if (programDetailsDict.TryGetValue(selectedProgramName, out var programDetail))
                {
                    departmentId = programDetail.DepartmentId;
                    _logger.LogInformation($"Mapped program \'{selectedProgramName}\' to Department ID: {departmentId}, Faculty: {programDetail.FacultyInstitute}"); 
                }
                else
                {
                     _logger.LogWarning($"Could not find details for program '{selectedProgramName}' with Language '{language}' and Level '{level}'.");
                }

                var programChoice = new ProgramChoice
                {
                    ProgramName = selectedProgramName,
                    Choice = i + 1,
                    ProgramSelection = programSelection,
                    DepartmentId = departmentId 
 
                };
                programSelection.ProgramChoices.Add(programChoice);
            }

            _context.ProgramSelections.Add(programSelection);
            await _context.SaveChangesAsync();
            return RedirectToAction("SelectPersonalInfoOption", new { id = programSelection.Id });
        }


        public async Task<IActionResult> PersonalInformation(int id, bool editMode = false)
        {
            var programSelection = await _context.ProgramSelections
                .Include(ps => ps.ProgramChoices)
                .FirstOrDefaultAsync(ps => ps.Id == id);

            if (programSelection == null)
            {
                return NotFound();
            }

            var personalInfo = await _context.PersonalInformations
                .FirstOrDefaultAsync(pi => pi.ProgramSelectionId == id);

            if (personalInfo != null)
            {
                _logger.LogInformation($"PersonalInformation GET - ID: {id} - CountryOfBirth: {personalInfo.CountryOfBirth}, CountryOfResidence: {personalInfo.CountryOfResidence}, Citizenship: {personalInfo.Citizenship}");
            }
            else
            {
                _logger.LogInformation($"PersonalInformation GET - ID: {id} - No personalInfo found in DB.");
            }

            if (personalInfo == null)
            {
                personalInfo = new PersonalInformation
                {
                    ProgramSelectionId = id
                };
                if (editMode && TempData["PreviousPersonalInfo"] != null)
                {
                    try
                    {
                        var previousInfoJson = TempData["PreviousPersonalInfo"] as string;
                        var previousInfo = System.Text.Json.JsonSerializer.Deserialize<PersonalInformation>(previousInfoJson);
                        
                        if (previousInfo != null)
                        {
                            personalInfo.FirstName = previousInfo.FirstName;
                            personalInfo.LastName = previousInfo.LastName;
                            personalInfo.PassportNumber = previousInfo.PassportNumber;
                            personalInfo.Gender = previousInfo.Gender;
                            personalInfo.DateOfBirth = previousInfo.DateOfBirth;
                            personalInfo.CountryOfBirth = previousInfo.CountryOfBirth;
                            personalInfo.CityOfBirth = previousInfo.CityOfBirth;
                            personalInfo.Citizenship = previousInfo.Citizenship;
                            personalInfo.Phone = previousInfo.Phone;
                            personalInfo.Address = previousInfo.Address;
                            personalInfo.CountryOfResidence = previousInfo.CountryOfResidence;
                            personalInfo.FatherName = previousInfo.FatherName;
                            personalInfo.MotherName = previousInfo.MotherName;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error deserializing previous personal info: {ex.Message}");
                    }
                }
            }

            return View(personalInfo);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PersonalInformation(PersonalInformation model)
        {
            Console.WriteLine("--------- DEBUG: PERSONAL INFORMATION POST STARTED ---------");
            Console.WriteLine($"ProgramSelectionId: {model.ProgramSelectionId}");
            Console.WriteLine($"FirstName: {model.FirstName}");
            Console.WriteLine($"LastName: {model.LastName}");
            Console.WriteLine($"Gender: {model.Gender}");
            Console.WriteLine($"DateOfBirth: {model.DateOfBirth}");
            Console.WriteLine($"CountryOfBirth: {model.CountryOfBirth}");
            Console.WriteLine($"CityOfBirth: {model.CityOfBirth}");
            Console.WriteLine($"Citizenship: {model.Citizenship}");
            Console.WriteLine($"Phone: {model.Phone}");
            Console.WriteLine($"CountryOfResidence: {model.CountryOfResidence}");
            Console.WriteLine($"Address: {model.Address}");
            Console.WriteLine($"FatherName: {model.FatherName}");
            Console.WriteLine($"MotherName: {model.MotherName}");

            var programSelectionInstance = await _context.ProgramSelections
                                               .Include(ps => ps.User)  
                                               .Include(ps => ps.ProgramChoices) 
                                               .FirstOrDefaultAsync(ps => ps.Id == model.ProgramSelectionId);

            if (programSelectionInstance == null)
            {
                _logger.LogError($"CRITICAL: ProgramSelection not found for ID: {model.ProgramSelectionId} at the beginning of PersonalInformation POST.");
                TempData["Error"] = "Application session could not be loaded. Please ensure you have an active application or try again.";
                return RedirectToAction("ProgramSelection"); 
            }

            if (programSelectionInstance.User != null && !string.IsNullOrEmpty(programSelectionInstance.User.Email))
            {
                model.Email = programSelectionInstance.User.Email;
                if (ModelState.ContainsKey(nameof(model.Email)))
                {
                    ModelState.Remove(nameof(model.Email));
                }
                _logger.LogInformation($"Assigned email '{model.Email}' from User '{programSelectionInstance.User.Id}' to PersonalInformation model for ProgramSelectionId: {model.ProgramSelectionId}");
            }
            else
            {
                _logger.LogWarning($"User or User.Email not found for ProgramSelectionId: {model.ProgramSelectionId}. Email field in model might remain unassigned.");
            }
            if (ModelState.ContainsKey(nameof(model.ProgramSelection))) 
            {
                ModelState.Remove(nameof(model.ProgramSelection));
                _logger.LogInformation($"Removed '{nameof(model.ProgramSelection)}' from ModelState for ProgramSelectionId: {model.ProgramSelectionId}");
            }
            if (!model.HasDualCitizenship)
            {
                model.Citizenship = model.CountryOfBirth;
                 _logger.LogInformation($"Dual citizenship not selected for ProgramSelectionId: {model.ProgramSelectionId}. Citizenship set to CountryOfBirth: {model.CountryOfBirth}.");
                 if (ModelState.ContainsKey(nameof(model.Citizenship)) && !string.IsNullOrEmpty(model.Citizenship))
                 {
                     ModelState.Remove(nameof(model.Citizenship));
                 }
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning($"PersonalInformation POST - ModelState invalid for ProgramSelectionId: {model.ProgramSelectionId}. Errors: {string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))}");
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList() });
                }
                return View(model); 
            }

            try
            {

                Console.WriteLine("Checking if record exists...");
                bool exists = await _context.PersonalInformations.AnyAsync(pi => pi.ProgramSelectionId == model.ProgramSelectionId);
                Console.WriteLine($"Record exists: {exists}");
                if (exists)
                {
                    Console.WriteLine($"Updating existing record for ProgramSelectionId: {model.ProgramSelectionId}");
                    var existingRecord = await _context.PersonalInformations.FirstOrDefaultAsync(pi => pi.ProgramSelectionId == model.ProgramSelectionId);
                    if (existingRecord != null)
                    {
                        Console.WriteLine($"Found existing record with ID: {existingRecord.Id}");
                        existingRecord.FirstName = model.FirstName;
                        existingRecord.LastName = model.LastName;
                        existingRecord.PassportNumber = model.PassportNumber;
                        existingRecord.Gender = model.Gender;
                        existingRecord.DateOfBirth = model.DateOfBirth;
                        existingRecord.CountryOfBirth = model.CountryOfBirth;
                        existingRecord.CityOfBirth = model.CityOfBirth;
                        existingRecord.Citizenship = model.Citizenship;
                        existingRecord.Phone = model.Phone;
                        existingRecord.Address = model.Address;
                        existingRecord.CountryOfResidence = model.CountryOfResidence;
                        existingRecord.FatherName = model.FatherName;
                        existingRecord.MotherName = model.MotherName;
                        _context.Update(existingRecord);
                        Console.WriteLine("Updated existing entity properties");
                    }
                    else
                    {
                        Console.WriteLine("Existing record query returned null, this shouldn't happen!");
                        model.Id = 0;
                        _context.Update(model);
                    }
                }
                else
                {
                    Console.WriteLine($"Adding new record for ProgramSelectionId: {model.ProgramSelectionId}");
                    model.Id = 0;
                    _context.PersonalInformations.Add(model);
                }
                Console.WriteLine("Saving changes to database...");
                try 
                {
                    await _context.SaveChangesAsync();
                    Console.WriteLine("Database operation successful");
                    if (programSelectionInstance.ApplicationStatus == "Needs Review")
                    {
                        Console.WriteLine("Application was previously sent back for review. Updating status to Submitted.");
                        programSelectionInstance.ApplicationStatus = "Submitted";
                        programSelectionInstance.ReviewComment = null; 
                        await _context.SaveChangesAsync();
                    }
                }
                catch (DbUpdateException dbUpdateEx)
                {
                    Console.WriteLine($"DbUpdateException: {dbUpdateEx.Message}");
                    if (dbUpdateEx.InnerException != null)
                    {
                        Console.WriteLine($"Inner exception: {dbUpdateEx.InnerException.Message}");
                    }
                    var entries = _context.ChangeTracker.Entries()
                        .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
                        .ToList();
                    Console.WriteLine($"Entries in change tracker: {entries.Count}");
                    foreach (var entry in entries)
                    {
                        Console.WriteLine($"Entity: {entry.Entity.GetType().Name}, State: {entry.State}");
                    }
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database error: {ex.Message}");
                Console.WriteLine($"Exception type: {ex.GetType().Name}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw; 
            }

            Console.WriteLine("Database operations completed successfully");
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, redirectUrl = Url.Action("EducationalInformation", new { id = model.ProgramSelectionId }) });
            }

            Console.WriteLine("--------- DEBUG: PERSONAL INFORMATION POST COMPLETED SUCCESSFULLY ---------");
            Console.WriteLine("Redirecting to Educational Information after successful save");
            return RedirectToAction("EducationalInformation", new { id = model.ProgramSelectionId });
        }
        public async Task<IActionResult> EducationalInformation(int id)
        {
            _logger.LogInformation($"EducationalInformation GET - ProgramSelectionId: {id}");

            var programSelection = await _context.ProgramSelections.FindAsync(id);
            if (programSelection == null)
            {
                _logger.LogWarning($"EducationalInformation GET: ProgramSelectionId {id} not found.");
                TempData["Error"] = "Application not found.";
                return RedirectToAction("MyApplications", "Home", new { area = "Candidate" });
            }

            var educationalInfo = await _context.EducationalInformations
                                        .FirstOrDefaultAsync(ei => ei.ProgramSelectionId == id);

            if (educationalInfo == null)
            {
                educationalInfo = new EducationalInformation { ProgramSelectionId = id };
            }

            ViewBag.DegreeOptions = new List<SelectListItem>
            {
                new SelectListItem { Value = "Bachelor Degree", Text = "Bachelor Degree" },
                new SelectListItem { Value = "Master Degree", Text = "Master Degree" },
                new SelectListItem { Value = "PhD Degree", Text = "PhD Degree" }
            };

            _logger.LogInformation($"Returning EducationalInformation view for ProgramSelectionId: {id}");
            return View(educationalInfo);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EducationalInformation(EducationalInformation model)
        {
            Console.WriteLine($"EducationalInformation POST başladı - ProgramSelectionId: {model.ProgramSelectionId}");
            if (Request.Form.ContainsKey("GPA"))
            {
                var gpaStr = Request.Form["GPA"].ToString().Replace(',', '.');
                if (decimal.TryParse(gpaStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal gpa))
                {
                    model.GPA = gpa;
                }
            }
            if (Request.Form.ContainsKey("LanguageExamScore"))
            {
                var scoreStr = Request.Form["LanguageExamScore"].ToString().Replace(',', '.');
                if (decimal.TryParse(scoreStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal score))
                {
                    model.LanguageExamScore = score;
                }
            }
            if (ModelState.ContainsKey("ProgramSelection"))
            {
                ModelState.Remove("ProgramSelection");
            }

            var programSelection = await _context.ProgramSelections
                .Include(ps => ps.ProgramChoices)
                .FirstOrDefaultAsync(ps => ps.Id == model.ProgramSelectionId);

            if (programSelection == null)
            {
                Console.WriteLine($"EducationalInformation POST: Program selection not found for ID: {model.ProgramSelectionId}");
                ModelState.AddModelError("", "Program selection not found.");
                return View(model);
            }
            model.ProgramSelection = programSelection;

            _logger.LogInformation($"EducationalInformation POST - ID: {model.ProgramSelectionId} - ModelState IsValid: {ModelState.IsValid} - Model.Country: {model.Country}");
            if (!ModelState.IsValid)
            {
                foreach (var state in ModelState)
                {
                    if (state.Value.Errors.Count > 0)
                    {
                        _logger.LogInformation($"EducationalInformation POST - ID: {model.ProgramSelectionId} - ModelState Error - Field: {state.Key}, Errors: {string.Join(", ", state.Value.Errors.Select(e => e.ErrorMessage))}");
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                var allErrors = new List<string>();
                foreach (var modelStateEntry in ModelState.Values)
                {
                    foreach (var error in modelStateEntry.Errors)
                    {
                        Console.WriteLine($"EducationalInformation POST - Validation Error: {error.ErrorMessage}");
                        allErrors.Add(error.ErrorMessage);
                    }
                }
                TempData["Error"] = string.Join("<br>", allErrors);
                return View(model);
            }

            try
            {
                Console.WriteLine($"Processing EducationalInformation POST for ProgramSelectionId: {model.ProgramSelectionId}");
                
                if (model.GPA < 0 || model.GPA > 4.0m)
                {
                    ModelState.AddModelError(nameof(model.GPA), "GPA must be between 0 and 4.0");
                    return View(model);
                }

                if (model.LanguageExamScore < 0 || model.LanguageExamScore > 100)
                {
                    ModelState.AddModelError(nameof(model.LanguageExamScore), "Language exam score must be between 0 and 100");
                    return View(model);
                }

                bool isUpdate = await _context.EducationalInformations.AnyAsync(ei => ei.ProgramSelectionId == model.ProgramSelectionId);
            
                _logger.LogInformation($"EducationalInformation POST - ID: {model.ProgramSelectionId} - Before Save. IsUpdate: {isUpdate}, Model.Country: {model.Country}");
                if(isUpdate)
                {
                    var existingForLog = await _context.EducationalInformations.AsNoTracking().FirstOrDefaultAsync(ei => ei.ProgramSelectionId == model.ProgramSelectionId);
                    if(existingForLog != null) _logger.LogInformation($"EducationalInformation POST - ID: {model.ProgramSelectionId} - Existing Country in DB (before attach/update): {existingForLog.Country}");
                }
                try
                {
                    if (isUpdate)
                    {
                        Console.WriteLine($"DEBUG: EducationalInformation (POST) - ID: {model.ProgramSelectionId} - Mevcut kayıt güncelleniyor.");
                        var existingRecord = await _context.EducationalInformations
                            .Include(ei => ei.ProgramSelection) 
                            .FirstOrDefaultAsync(ei => ei.ProgramSelectionId == model.ProgramSelectionId);
                            
                        if (existingRecord != null)
                        {
                            existingRecord.SchoolName = model.SchoolName;
                            existingRecord.GraduationYear = model.GraduationYear;
                            existingRecord.GPA = model.GPA;
                            existingRecord.Country = model.Country; 
                            existingRecord.City = model.City;
                            existingRecord.LanguageProficiency = model.LanguageProficiency;
                            existingRecord.LanguageExamScore = model.LanguageExamScore;
                            existingRecord.IsBlueCardOwner = model.IsBlueCardOwner;
                            
                            if (existingRecord.ProgramSelection == null) 
                            {
                                Console.WriteLine($"DEBUG: EducationalInformation (POST) - ID: {model.ProgramSelectionId} - existingRecord.ProgramSelection is null, re-fetching.");
                                existingRecord.ProgramSelection = await _context.ProgramSelections
                                    .FirstOrDefaultAsync(ps => ps.Id == model.ProgramSelectionId);
                            }
                            Console.WriteLine($"DEBUG: EducationalInformation (POST) - ID: {model.ProgramSelectionId} - Mevcut kayıt ({existingRecord.Id}) güncellenmeye hazırlandı. Country: {existingRecord.Country}");
                        }
                        else
                        {
                            Console.WriteLine($"DEBUG: EducationalInformation (POST) - ID: {model.ProgramSelectionId} - Mevcut kayıt bulunamadı, yenisi oluşturuluyor (bu durum beklenmiyor).");
                            model.Id = 0; 
                            model.ProgramSelection = await _context.ProgramSelections
                                .FirstOrDefaultAsync(ps => ps.Id == model.ProgramSelectionId);
                            _context.EducationalInformations.Add(model);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Adding new EducationalInformation for ProgramSelectionId: {model.ProgramSelectionId}");
                        model.Id = 0; 
                        model.ProgramSelection = await _context.ProgramSelections
                            .FirstOrDefaultAsync(ps => ps.Id == model.ProgramSelectionId);
                            
                        _context.EducationalInformations.Add(model);
                    }
                    
                    await _context.SaveChangesAsync();
                    Console.WriteLine("EducationalInformation saved successfully");
                }
                catch (Exception dbEx)
                {
                    Console.WriteLine($"Database error in EducationalInformation POST: {dbEx.Message}");
                    Console.WriteLine($"Stack trace: {dbEx.StackTrace}");
                    throw;
                }

                Console.WriteLine("Redirecting to Documents page");
                return RedirectToAction("Documents", new { id = model.ProgramSelectionId });
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error type in EducationalInformation POST: {ex.GetType().Name}");
                Console.WriteLine($"Error in EducationalInformation POST: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    Console.WriteLine($"Inner exception stack trace: {ex.InnerException.StackTrace}");
                }
                
                ModelState.AddModelError("", "An error occurred while saving your information. Please try again.");
                return View(model);
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetAvailablePrograms(string language, string level)
        {
            var trimmedLanguage = language?.Trim();
            var trimmedLevel = level?.Trim();

            if (string.IsNullOrEmpty(trimmedLanguage) || string.IsNullOrEmpty(trimmedLevel))
            {
                return Json(new List<object>()); 
            }
            var lowerLanguage = trimmedLanguage.ToLower();
            var lowerLevel = trimmedLevel.ToLower();

            var programsQuery = _context.Programs
                .Where(p => p.Language != null && p.Language.ToLower() == lowerLanguage &&
                            p.Level != null && p.Level.ToLower() == lowerLevel)
                .OrderBy(p => p.Name);
            var programs = await programsQuery.Select(p => new
            {
                ProgramId = p.ProgramId, 
                Name = p.Name, 
                NameEn = p.NameEn,
                DisplayName = (trimmedLanguage.Equals("English", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(p.NameEn))
                                ? $"{p.Name} / {p.NameEn}" 
                                : p.Name,
                FacultyInstitute = trimmedLanguage.Equals("English", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(p.FacultyInstituteEn)
                                        ? p.FacultyInstituteEn
                                        : p.FacultyInstitute
            }).ToListAsync();

        _logger.LogInformation($"GetAvailablePrograms - Found {programs.Count} programs for Lang: {language}, Level: {level}"); 

        if (!programs.Any())
        {
            var messageResponse = new { message = "No programs available for the selected combination" };
            _logger.LogInformation($"GetAvailablePrograms - Returning message: {messageResponse.message}");
            return Json(messageResponse);
        }

        if(programs.Any()) {
             _logger.LogInformation($"GetAvailablePrograms - First program details: Name='{programs[0].Name}', Faculty='{programs[0].FacultyInstitute}'");
        }

        _logger.LogInformation($"GetAvailablePrograms - Returning program list."); 
            return Json(programs);
        }

        public async Task<IActionResult> Documents(int id)
        {
            try
            {
                _logger.LogInformation($"Documents GET - ProgramSelectionId: {id}"); 
                
                var programSelection = await _context.ProgramSelections
                    .Include(ps => ps.User) 
                    .Include(ps => ps.ProgramChoices)
                    .FirstOrDefaultAsync(ps => ps.Id == id);

                if (programSelection == null)
                {
                    _logger.LogWarning($"Documents GET: Program selection not found for ID: {id}");
                    TempData["Error"] = "Application not found.";
                    return RedirectToAction("MyApplications", "Home", new { area = "Candidate" }); 
                }

                var personalInfo = await _context.PersonalInformations
                                    .FirstOrDefaultAsync(pi => pi.ProgramSelectionId == programSelection.Id);
                
                if (personalInfo == null)
                {
                    _logger.LogWarning($"PersonalInformation not found for ProgramSelectionId: {id}, redirecting to PersonalInformation page");
                    TempData["Error"] = "Please complete your personal information first.";
                    return RedirectToAction("PersonalInformation", new { id = id });
                }

                var educationalInfo = await _context.EducationalInformations
                    .FirstOrDefaultAsync(ei => ei.ProgramSelectionId == id);

                if (educationalInfo == null)
                {
                    _logger.LogWarning($"Educational Information not found for ProgramSelectionId: {id}, redirecting to EducationalInformation page");
                    TempData["Error"] = "Please complete your educational information first.";
                    return RedirectToAction("EducationalInformation", new { id = id });
                }

                var requiredDocTypes = new List<string>();
                var optionalDocTypes = new List<string>();

                requiredDocTypes.Add("Diploma");
                requiredDocTypes.Add("Transcript");
                bool requiresPassport = (personalInfo.CountryOfBirth?.ToLower() != "turkey") && 
                                        (personalInfo.Citizenship?.ToLower() != "turkey");
                if (requiresPassport)
                {
                    requiredDocTypes.Add("Passport");
                }
                bool isTurkishStudent = personalInfo.CountryOfBirth?.ToLower() == "turkey";
                bool isTurkishProgram = programSelection.Language?.Equals("Turkish", StringComparison.OrdinalIgnoreCase) == true;

                if (programSelection.ProgramLevel == "MasterThesis")
                {
                    if (isTurkishStudent)
                    {
                        requiredDocTypes.Add("ALES Result"); 
                    }
                    if (!isTurkishProgram) 
                    {
                    requiredDocTypes.Add("Language Exam Result");
                    }
                }
                else if (programSelection.ProgramLevel == "PhD")
                {
                    if (isTurkishStudent)
                    {
                        requiredDocTypes.Add("ALES Result"); 
                    }
                    if (!isTurkishProgram)
                    {
                    requiredDocTypes.Add("Language Exam Result"); 
                    }
                    requiredDocTypes.Add("Master Thesis Cover Page");
                }
                optionalDocTypes.Add("Reference Letter 1");
                optionalDocTypes.Add("Reference Letter 2");

                ViewBag.AllPossibleDocumentTypes = new List<string> {
                    "Passport", "Diploma", "Transcript", "ALES Result", "Language Exam Result",
                    "Master Thesis Cover Page", "Reference Letter 1", "Reference Letter 2"
                }.Distinct().ToList();

                var uploadedDocuments = await _context.Documents
                    .Where(d => d.ProgramSelectionId == id)
                    .ToListAsync();
                    
                ViewBag.UploadedDocuments = uploadedDocuments; 
                
                var uploadedDocTypes = uploadedDocuments.Select(d => d.DocumentType).ToList();
                var missingRequiredDocTypes = requiredDocTypes.Except(uploadedDocTypes).ToList();
                
                ViewBag.RequiredDocTypes = requiredDocTypes;
                ViewBag.OptionalDocTypes = optionalDocTypes;
                ViewBag.MissingRequiredDocTypes = missingRequiredDocTypes; 

                _logger.LogInformation($"Showing Documents page for ProgramSelectionId: {id}. ProgramLevel: {programSelection.ProgramLevel}, CountryOfBirth: {personalInfo.CountryOfBirth}");
                _logger.LogInformation($"Required Docs: {string.Join(", ", requiredDocTypes)}. Optional Docs: {string.Join(", ", optionalDocTypes)}. Missing Required: {string.Join(", ", missingRequiredDocTypes)}");

                return View(new DocumentsViewModel { ProgramSelectionId = id });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Documents GET for ID {id}: {ex.Message}", ex); 
                TempData["Error"] = "An unexpected error occurred while preparing the documents page.";
                return RedirectToAction("MyApplications", "Home", new { area = "Candidate" }); 
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadDocument(IFormCollection formCollection)
        {
            try
            {
                Console.WriteLine("=========== UploadDocument POST STARTED ===========");
                
                if (!int.TryParse(formCollection["ProgramSelectionId"], out int programSelectionId))
                {
                    Console.WriteLine("ProgramSelectionId çözümlenemedi");
                    
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = false, error = "Invalid program selection ID." });
                    }
                    
                    return RedirectToAction("ProgramSelection");
                }

                string documentType = formCollection["DocumentType"];
                IFormFile documentFile = formCollection.Files.FirstOrDefault();

                Console.WriteLine($"UploadDocument POST - ProgramSelectionId: {programSelectionId}, DocumentType: {documentType}");
                Console.WriteLine($"Form Collection Keys: {string.Join(", ", formCollection.Keys)}");
                Console.WriteLine($"Files Count: {formCollection.Files.Count}");
                foreach (var key in formCollection.Keys)
                {
                    Console.WriteLine($"Form key: {key}, Value: {formCollection[key]}");
                }
                
                if (formCollection.Files.Count > 0)
                {
                    foreach (var file in formCollection.Files)
                    {
                        Console.WriteLine($"File: {file.Name}, Filename: {file.FileName}, Length: {file.Length}, ContentType: {file.ContentType}");
                    }
                }
                else
                {
                    Console.WriteLine("No files found in the request");
                    Console.WriteLine($"Request content type: {Request.ContentType}");
                    Console.WriteLine($"Request form file collection count: {Request.Form.Files.Count}");
                    
                    if (Request.Form.Files.GetFile("documentFile") != null)
                    {
                        documentFile = Request.Form.Files.GetFile("documentFile");
                        Console.WriteLine($"Found documentFile directly from Request.Form.Files: {documentFile.FileName}");
                    }
                    else
                    {
                        Console.WriteLine("documentFile not found in Request.Form.Files");
                    }
                }
                
                if (string.IsNullOrEmpty(documentType) || documentFile == null || documentFile.Length <= 0)
                {
                    Console.WriteLine("Dosya yok veya belge tipi belirtilmemiş");
                    Console.WriteLine($"DocumentType: {documentType}");
                    Console.WriteLine($"DocumentFile: {(documentFile != null ? "not null" : "null")}");
                    if (documentFile != null)
                    {
                        Console.WriteLine($"DocumentFile Length: {documentFile.Length}");
                    }
                    
                    string errorMessage = "Please select a document type and file to upload";
                    
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        Console.WriteLine("Sending AJAX error response: " + errorMessage);
                        return Json(new { success = false, error = errorMessage });
                    }
                    
                    TempData["Error"] = errorMessage;
                    return RedirectToAction("Documents", new { id = programSelectionId });
                }

                var programSelection = await _context.ProgramSelections
                    .FirstOrDefaultAsync(ps => ps.Id == programSelectionId);

                if (programSelection == null)
                {
                    Console.WriteLine($"UploadDocument POST: Program selection not found for ID: {programSelectionId}");
                    
                    string errorMessage = "Program selection not found.";
                    
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = false, error = errorMessage });
                    }
                    
                    TempData["Error"] = errorMessage;
                    return RedirectToAction("ProgramSelection");
                }
                var existingDoc = await _context.Documents
                    .FirstOrDefaultAsync(d => d.ProgramSelectionId == programSelectionId && d.DocumentType == documentType);

                if (existingDoc != null)
                {
                    Console.WriteLine($"Bu tip belge zaten yüklenmiş: {documentType}");
                    
                    string errorMessage = $"A document of type {documentType} is already uploaded. Please delete the existing one first.";
                    
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = false, error = errorMessage });
                    }
                    
                    TempData["Error"] = errorMessage;
                    return RedirectToAction("Documents", new { id = programSelectionId });
                }
                if (documentFile.Length > 10 * 1024 * 1024)
                {
                    Console.WriteLine("Dosya boyutu 10MB'dan büyük");
                    
                    string errorMessage = "File size exceeds the limit of 10MB.";
                    
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = false, error = errorMessage });
                    }
                    
                    TempData["Error"] = errorMessage;
                    return RedirectToAction("Documents", new { id = programSelectionId });
                }

                try
                {
                    Console.WriteLine("Dosya içeriğini okumaya başlıyorum...");
                    byte[] fileContent;
                    using (var memoryStream = new MemoryStream())
                    {
                        await documentFile.CopyToAsync(memoryStream);
                        fileContent = memoryStream.ToArray();
                    }
                    Console.WriteLine($"Dosya içeriği okundu: {fileContent.Length} bytes");
                    var document = new Document
                    {
                        DocumentType = documentType,
                        FileName = Guid.NewGuid().ToString() + Path.GetExtension(documentFile.FileName),
                        OriginalFileName = documentFile.FileName,
                        ContentType = documentFile.ContentType,
                        FileSize = documentFile.Length,
                        FileContent = fileContent,
                        UploadDate = DateTime.UtcNow,
                        ProgramSelectionId = programSelectionId
                    };

                    Console.WriteLine("Belge nesnesini veritabanına kaydetmeye başlıyorum...");
                    _context.Documents.Add(document);
                    await _context.SaveChangesAsync();

                    Console.WriteLine($"Belge başarıyla yüklendi: {documentType}");
                    
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = true, message = $"{documentType} uploaded successfully." });
                    }
                    
                    TempData["Success"] = $"{documentType} uploaded successfully.";
                    Console.WriteLine("=========== UploadDocument POST COMPLETED SUCCESSFULLY ===========");
                    return RedirectToAction("Documents", new { id = programSelectionId });
                }
                catch (Exception innerEx)
                {
                    Console.WriteLine($"Dosya işleme hatası: {innerEx.Message}");
                    Console.WriteLine($"İç hata ayrıntıları: {innerEx.StackTrace}");
                    throw; 
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UploadDocument: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                string errorMessage = $"An error occurred while uploading your document: {ex.Message}";
                
                int programSelectionId = 0;
                if (formCollection != null && formCollection.ContainsKey("ProgramSelectionId") && 
                    int.TryParse(formCollection["ProgramSelectionId"], out programSelectionId))
                {
                    Console.WriteLine($"ProgramSelectionId found in exception handler: {programSelectionId}");
                }
                
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    Console.WriteLine("Sending AJAX error response for exception: " + errorMessage);
                    return Json(new { success = false, error = errorMessage });
                }
                
                Console.WriteLine("=========== UploadDocument POST FAILED ===========");
                TempData["Error"] = errorMessage;
                
                if (programSelectionId > 0)
                {
                    return RedirectToAction("Documents", new { id = programSelectionId });
                }
                
                return RedirectToAction("ProgramSelection");
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDocument(IFormCollection formCollection)
        {
            try
            {
                if (!int.TryParse(formCollection["id"], out int id) || 
                    !int.TryParse(formCollection["programSelectionId"], out int programSelectionId))
                {
                    Console.WriteLine("ID veya ProgramSelectionId çözümlenemedi");
                    return RedirectToAction("ProgramSelection");
                }

                Console.WriteLine($"DeleteDocument POST - DocumentId: {id}, ProgramSelectionId: {programSelectionId}");
                
                var document = await _context.Documents.FindAsync(id);
                
                if (document == null)
                {
                    Console.WriteLine($"Document not found for ID: {id}");
                    TempData["Error"] = "Document not found.";
                    return RedirectToAction("Documents", new { id = programSelectionId });
                }

                if (document.ProgramSelectionId != programSelectionId)
                {
                    Console.WriteLine($"Document does not belong to the program selection: {programSelectionId}");
                    TempData["Error"] = "Invalid document request.";
                    return RedirectToAction("Documents", new { id = programSelectionId });
                }

                _context.Documents.Remove(document);
                await _context.SaveChangesAsync();

                Console.WriteLine($"Belge başarıyla silindi: {document.DocumentType}");
                TempData["Success"] = $"{document.DocumentType} deleted successfully.";
                return RedirectToAction("Documents", new { id = programSelectionId });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DeleteDocument: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                TempData["Error"] = $"An error occurred while deleting the document: {ex.Message}";
                
                int programSelectionId = 0;
                if (formCollection != null && formCollection.ContainsKey("programSelectionId") && 
                    int.TryParse(formCollection["programSelectionId"], out programSelectionId))
                {
                    return RedirectToAction("Documents", new { id = programSelectionId });
                }
                
                return RedirectToAction("ProgramSelection");
            }
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DocumentsComplete(IFormCollection formCollection)
        {
            try
            {
                if (!int.TryParse(formCollection["ProgramSelectionId"], out int programSelectionId))
                {
                    _logger.LogWarning("DocumentsComplete POST: ProgramSelectionId could not be parsed from form.");
                    TempData["Error"] = "Invalid application identifier.";
                    return RedirectToAction("MyApplications", "Home", new { area = "Candidate" });
                }

                _logger.LogInformation($"DocumentsComplete POST - ProgramSelectionId: {programSelectionId}");
                
                var programSelection = await _context.ProgramSelections
                    .Include(ps => ps.User) 
                    .FirstOrDefaultAsync(ps => ps.Id == programSelectionId);

                if (programSelection == null)
                {
                    _logger.LogWarning($"DocumentsComplete POST: Program selection not found for ID: {programSelectionId}");
                    TempData["Error"] = "Application not found.";
                    return RedirectToAction("MyApplications", "Home", new { area = "Candidate" });
                }

                var personalInfo = await _context.PersonalInformations
                                    .FirstOrDefaultAsync(pi => pi.ProgramSelectionId == programSelection.Id);
                
                if (personalInfo == null) 
                {
                    _logger.LogError($"DocumentsComplete POST: PersonalInformation not found for ProgramSelectionId: {programSelectionId}. This should have been caught earlier.");
                    TempData["Error"] = "Critical application data is missing. Please go back and complete your personal information.";
                    return RedirectToAction("PersonalInformation", new { id = programSelectionId });
                }
                var currentRequiredDocTypes = new List<string>();
                currentRequiredDocTypes.Add("Diploma");
                currentRequiredDocTypes.Add("Transcript");

                bool requiresPassportForCompletion = (personalInfo.CountryOfBirth?.ToLower() != "turkey") && 
                                                     (personalInfo.Citizenship?.ToLower() != "turkey");
                if (requiresPassportForCompletion)
                {
                    currentRequiredDocTypes.Add("Passport");
                }
                bool isTurkishStudentForCompletion = personalInfo.CountryOfBirth?.ToLower() == "turkey";
                bool isTurkishProgramForCompletion = programSelection.Language?.Equals("Turkish", StringComparison.OrdinalIgnoreCase) == true;

                if (programSelection.ProgramLevel == "MasterThesis")
                {
                    if (isTurkishStudentForCompletion)
                {
                    currentRequiredDocTypes.Add("ALES Result");
                    }
                    if (!isTurkishProgramForCompletion)
                    {
                    currentRequiredDocTypes.Add("Language Exam Result");
                    }
                }
                else if (programSelection.ProgramLevel == "PhD")
                {
                    if (isTurkishStudentForCompletion)
                {
                    currentRequiredDocTypes.Add("ALES Result");
                    }
                    if (!isTurkishProgramForCompletion)
                    {
                    currentRequiredDocTypes.Add("Language Exam Result");
                    }
                    currentRequiredDocTypes.Add("Master Thesis Cover Page");
                }
                
                var uploadedDocuments = await _context.Documents
                    .Where(d => d.ProgramSelectionId == programSelectionId)
                    .Select(d => d.DocumentType) 
                    .ToListAsync();
                
                _logger.LogInformation($"DocumentsComplete - ProgramSelectionId: {programSelectionId}. Required: {string.Join(",", currentRequiredDocTypes)}. Uploaded: {string.Join(",", uploadedDocuments)}");

                var missingRequiredDocuments = currentRequiredDocTypes.Except(uploadedDocuments).ToList();

                if (missingRequiredDocuments.Any())
                {
                    _logger.LogWarning($"DocumentsComplete - Missing required documents for {programSelectionId}: {string.Join(", ", missingRequiredDocuments)}");
                    TempData["Error"] = "Please upload all required documents before proceeding. Missing: " + string.Join(", ", missingRequiredDocuments);
                    return RedirectToAction("Documents", new { id = programSelectionId });
                }

                _logger.LogInformation($"DocumentsComplete - All required documents present for {programSelectionId}. Redirecting to ReviewApplication.");
                return RedirectToAction("ReviewApplication", new { id = programSelectionId });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in DocumentsComplete: {ex.Message}", ex);
                TempData["Error"] = "An error occurred while finalizing your documents. Please try again.";
                
                int programSelectionIdFromForm = 0;
                if (formCollection != null && formCollection.ContainsKey("ProgramSelectionId") && 
                    int.TryParse(formCollection["ProgramSelectionId"], out programSelectionIdFromForm) && programSelectionIdFromForm > 0)
                {
                    return RedirectToAction("Documents", new { id = programSelectionIdFromForm });
                }
                
                return RedirectToAction("MyApplications", "Home", new { area = "Candidate" });
            }
        }
        public async Task<IActionResult> ReviewApplication(int id)
        {
            try
            {
                Console.WriteLine($"ReviewApplication GET - ProgramSelectionId: {id}");
                
                var programSelection = await _context.ProgramSelections
                    .Include(ps => ps.ProgramChoices)
                    .FirstOrDefaultAsync(ps => ps.Id == id);

                if (programSelection == null)
                {
                    Console.WriteLine($"Program selection not found for ID: {id}");
                    return RedirectToAction("ProgramSelection");
                }
                var personalInfo = await _context.PersonalInformations
                    .FirstOrDefaultAsync(pi => pi.ProgramSelectionId == id);
                    
                var educationalInfo = await _context.EducationalInformations
                    .FirstOrDefaultAsync(ei => ei.ProgramSelectionId == id);
                    
                var documents = await _context.Documents
                    .Where(d => d.ProgramSelectionId == id)
                    .ToListAsync();

                if (personalInfo == null || educationalInfo == null)
                {
                    Console.WriteLine($"Missing personal or educational information for ID: {id}");
                    TempData["Error"] = "Your application is incomplete. Please complete all required sections.";
                    return RedirectToAction("ProgramSelection");
                }

                ViewBag.PersonalInfo = personalInfo;
                ViewBag.EducationalInfo = educationalInfo;
                ViewBag.Documents = documents;

                return View("ApplicationCompleted", programSelection);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ReviewApplication: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                TempData["Error"] = $"An error occurred: {ex.Message}";
                return RedirectToAction("ProgramSelection");
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitApplication(IFormCollection formCollection)
        {
            try
            {
                if (!int.TryParse(formCollection["ProgramSelectionId"], out int programSelectionId))
                {
                    Console.WriteLine("ProgramSelectionId çözümlenemedi");
                    return RedirectToAction("ProgramSelection");
                }

                Console.WriteLine($"SubmitApplication POST - ProgramSelectionId: {programSelectionId}");
                
                var programSelection = await _context.ProgramSelections
                    .FirstOrDefaultAsync(ps => ps.Id == programSelectionId);

                if (programSelection == null)
                {
                    Console.WriteLine($"Program selection not found for ID: {programSelectionId}");
                    return RedirectToAction("ProgramSelection");
                }
                programSelection.ApplicationStatus = "Submitted";
                if (string.IsNullOrEmpty(programSelection.TrackingNumber))
                {
                    programSelection.TrackingNumber = GenerateTrackingNumber();
                }
                
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Your application has been submitted successfully!";
                TempData["TrackingNumber"] = programSelection.TrackingNumber;
                
                return RedirectToAction("ApplicationSubmitted", new { id = programSelectionId });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SubmitApplication: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                TempData["Error"] = $"An error occurred while submitting your application: {ex.Message}";
                
                int programSelectionId = 0;
                if (formCollection != null && formCollection.ContainsKey("ProgramSelectionId") && 
                    int.TryParse(formCollection["ProgramSelectionId"], out programSelectionId))
                {
                    return RedirectToAction("ReviewApplication", new { id = programSelectionId });
                }
                
                return RedirectToAction("ProgramSelection");
            }
        }
        public async Task<IActionResult> ApplicationSubmitted(int id)
        {
            var programSelection = await _context.ProgramSelections
                .Include(ps => ps.ProgramChoices)
                .FirstOrDefaultAsync(ps => ps.Id == id);

            if (programSelection == null)
            {
                return RedirectToAction("ProgramSelection");
            }
            
            ViewBag.TrackingNumber = programSelection.TrackingNumber;
            return View(programSelection);
        }
        public async Task<IActionResult> ViewDocument(int id)
        {
            try
            {
                Console.WriteLine($"ViewDocument GET - DocumentId: {id}");
                
                var document = await _context.Documents.FindAsync(id);
                
                if (document == null)
                {
                    Console.WriteLine($"Document not found for ID: {id}");
                    TempData["Error"] = "Document not found.";
                    return RedirectToAction("ProgramSelection");
                }
                Console.WriteLine($"Serving document: {document.OriginalFileName}, Type: {document.ContentType}, Size: {document.FileSize} bytes");
                return File(document.FileContent, document.ContentType, document.OriginalFileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ViewDocument: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                TempData["Error"] = $"An error occurred: {ex.Message}";
                return RedirectToAction("ProgramSelection");
            }
        }
        [Authorize(Roles = "Candidate")]
        public async Task<IActionResult> MyApplications()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }
            
            try
            {
                var applications = await _context.ProgramSelections
                    .Include(ps => ps.ProgramChoices)
                    .Where(ps => ps.UserId == userId)
                    .OrderByDescending(ps => ps.CreatedAt)
                    .ToListAsync();
                
                return View(applications);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in MyApplications: {ex.Message}");
                TempData["Error"] = "An error occurred while retrieving your applications.";
                return RedirectToAction("Index", "Home", new { area = "Candidate" });
            }
        }
        [Authorize(Roles = "Candidate")]
        public async Task<IActionResult> SelectPersonalInfoOption(int id)
        {
            var programSelection = await _context.ProgramSelections
                .Include(ps => ps.ProgramChoices)
                .FirstOrDefaultAsync(ps => ps.Id == id);

            if (programSelection == null)
            {
                _logger.LogWarning($"ProgramSelection not found for ID: {id}");
                TempData["Error"] = "Application not found.";
                return RedirectToAction("ProgramSelection");
            }
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var previousApplications = await _context.ProgramSelections
                .Where(ps => ps.UserId == userId && ps.Id != id) 
                .Where(ps => ps.ApplicationStatus != "Draft") 
                .OrderByDescending(ps => ps.CreatedAt)
                .ToListAsync();
            var latestCompletedApplication = previousApplications.FirstOrDefault();

            if (latestCompletedApplication != null)
            {
                var personalInfo = await _context.PersonalInformations
                    .FirstOrDefaultAsync(pi => pi.ProgramSelectionId == latestCompletedApplication.Id);

                if (personalInfo != null)
                {
                    ViewBag.HasPreviousApplication = true;
                    ViewBag.PreviousPersonalInfo = personalInfo;
                    ViewBag.LatestApplicationId = latestCompletedApplication.Id;
                }
                else
                {
                    ViewBag.HasPreviousApplication = false;
                }
            }
            else
            {
                ViewBag.HasPreviousApplication = false;
            }

            return View(programSelection);
        }
        [HttpPost]
        [Authorize(Roles = "Candidate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UseExistingPersonalInfo(int id, int previousApplicationId)
        {
            var programSelection = await _context.ProgramSelections
                .Include(ps => ps.ProgramChoices)
                .FirstOrDefaultAsync(ps => ps.Id == id);
                
            if (programSelection == null)
            {
                TempData["Error"] = "Application not found.";
                return RedirectToAction("ProgramSelection");
            }
            var previousPersonalInfo = await _context.PersonalInformations
                .FirstOrDefaultAsync(pi => pi.ProgramSelectionId == previousApplicationId);

            if (previousPersonalInfo == null)
            {
                TempData["Error"] = "Previous personal information not found.";
                return RedirectToAction("PersonalInformation", new { id = id });
            }
            var newPersonalInfo = new PersonalInformation
            {
                ProgramSelectionId = id,
                FirstName = previousPersonalInfo.FirstName,
                LastName = previousPersonalInfo.LastName,
                PassportNumber = previousPersonalInfo.PassportNumber,
                Gender = previousPersonalInfo.Gender,
                DateOfBirth = previousPersonalInfo.DateOfBirth,
                CountryOfBirth = previousPersonalInfo.CountryOfBirth,
                CityOfBirth = previousPersonalInfo.CityOfBirth,
                Citizenship = previousPersonalInfo.Citizenship,
                Phone = previousPersonalInfo.Phone,
                Address = previousPersonalInfo.Address,
                CountryOfResidence = previousPersonalInfo.CountryOfResidence,
                FatherName = previousPersonalInfo.FatherName,
                MotherName = previousPersonalInfo.MotherName
            };

            _context.PersonalInformations.Add(newPersonalInfo);
            await _context.SaveChangesAsync();
            return RedirectToAction("EducationalInformation", new { id = id });
        }
        [HttpPost]
        [Authorize(Roles = "Candidate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditExistingPersonalInfo(int id, int previousApplicationId)
        {
            var programSelection = await _context.ProgramSelections
                .Include(ps => ps.ProgramChoices)
                .FirstOrDefaultAsync(ps => ps.Id == id);
                
            if (programSelection == null)
            {
                TempData["Error"] = "Application not found.";
                return RedirectToAction("ProgramSelection");
            }
            var previousPersonalInfo = await _context.PersonalInformations
                .FirstOrDefaultAsync(pi => pi.ProgramSelectionId == previousApplicationId);

            if (previousPersonalInfo == null)
            {
                TempData["Error"] = "Previous personal information not found.";
                return RedirectToAction("PersonalInformation", new { id = id });
            }
            TempData["PreviousPersonalInfo"] = System.Text.Json.JsonSerializer.Serialize(previousPersonalInfo);
            return RedirectToAction("PersonalInformation", new { id = id, editMode = true });
        }
        [HttpPost]
        [Authorize(Roles = "Candidate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UseNewPersonalInfo(int id)
        {
            var programSelection = await _context.ProgramSelections
                .Include(ps => ps.ProgramChoices)
                .FirstOrDefaultAsync(ps => ps.Id == id);
                
            if (programSelection == null)
            {
                TempData["Error"] = "Application not found.";
                return RedirectToAction("ProgramSelection");
            }
            return RedirectToAction("PersonalInformation", new { id = id });
        }
        [HttpPost]
        [Authorize(Roles = "Candidate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteApplication(int id)
        {
            try
            {
                _logger.LogInformation($"DeleteApplication POST - ID: {id}");
                var application = await _context.ProgramSelections
                    .FirstOrDefaultAsync(ps => ps.Id == id);

                if (application == null)
                {
                    _logger.LogWarning($"Application not found for ID: {id}");
                    TempData["Error"] = "Application not found.";
                    return RedirectToAction("MyApplications");
                }
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (application.UserId != userId)
                {
                    _logger.LogWarning($"User {userId} attempted to delete application {id} belonging to user {application.UserId}");
                    TempData["Error"] = "You are not authorized to delete this application.";
                    return RedirectToAction("MyApplications");
                }
                if (application.ApplicationStatus != "Draft")
                {
                    _logger.LogWarning($"Attempted to delete non-draft application {id} with status {application.ApplicationStatus}");
                    TempData["Error"] = "Only draft applications can be deleted.";
                    return RedirectToAction("MyApplications");
                }
                var documents = await _context.Documents
                    .Where(d => d.ProgramSelectionId == id)
                    .ToListAsync();
                _context.Documents.RemoveRange(documents);
                var educationalInfo = await _context.EducationalInformations
                    .FirstOrDefaultAsync(ei => ei.ProgramSelectionId == id);
                if (educationalInfo != null)
                {
                    _context.EducationalInformations.Remove(educationalInfo);
                }
                var personalInfo = await _context.PersonalInformations
                    .FirstOrDefaultAsync(pi => pi.ProgramSelectionId == id);
                if (personalInfo != null)
                {
                    _context.PersonalInformations.Remove(personalInfo);
                }
                var programChoices = await _context.Set<ProgramChoice>()
                    .Where(pc => pc.ProgramSelectionId == id)
                    .ToListAsync();
                _context.RemoveRange(programChoices);
                _context.ProgramSelections.Remove(application);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Application has been deleted successfully.";
                return RedirectToAction("MyApplications");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in DeleteApplication: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                
                TempData["Error"] = "An error occurred while deleting the application.";
                return RedirectToAction("MyApplications");
            }
        }
        private string GenerateTrackingNumber()
        {
            var random = new Random();
            var randomNumber = random.Next(100000, 999999);
            return $"AP{randomNumber}";
        }
    }
} 