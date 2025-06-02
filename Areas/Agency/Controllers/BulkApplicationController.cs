using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GraduationAdmissionSystem.ViewModels;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using GraduationAdmissionSystem.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using GraduationAdmissionSystem.Models;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using System.IO;

namespace GraduationAdmissionSystem.Areas.Agency.Controllers
{
    [Area("Agency")]
    [Authorize(Roles = "Agency")]
    public class BulkApplicationController : Controller
    {
        private const string BulkApplicationSessionKey = "BulkApplication";
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<BulkApplicationController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public BulkApplicationController(
            IHttpContextAccessor httpContextAccessor, 
            ILogger<BulkApplicationController> logger, 
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _context = context;
            _userManager = userManager;
        }

        private BulkApplicationViewModel GetBulkApplicationFromSession()
        {
            var session = _httpContextAccessor.HttpContext.Session;
            var jsonData = session.GetString(BulkApplicationSessionKey);
            if (string.IsNullOrEmpty(jsonData))
            {
                return new BulkApplicationViewModel();
            }
            try
            {
                var options = new JsonSerializerOptions
                {
                    ReferenceHandler = ReferenceHandler.Preserve
                };
                return JsonSerializer.Deserialize<BulkApplicationViewModel>(jsonData, options);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error deserializing BulkApplicationViewModel from session.");
                return new BulkApplicationViewModel(); 
            }
        }

        private void SaveBulkApplicationToSession(BulkApplicationViewModel model)
        {
            var session = _httpContextAccessor.HttpContext.Session;
            var options = new JsonSerializerOptions
            {
                ReferenceHandler = ReferenceHandler.Preserve,
                
            };
            var jsonData = JsonSerializer.Serialize(model, options);
            session.SetString(BulkApplicationSessionKey, jsonData);
        }

        private void PopulateViewBagForProgramSelection(StudentApplicationItem? studentItem = null)
        {
            var programLevels = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "Select Program Level", Disabled = true, Selected = string.IsNullOrEmpty(studentItem?.ProgramSelectionData?.ProgramLevel) },
                new SelectListItem { Value = "MasterThesis", Text = "Master with Thesis" },
                new SelectListItem { Value = "MasterNonThesis", Text = "Master without Thesis" },
                new SelectListItem { Value = "PhD", Text = "Doctorate (PhD)" }
            };
            ViewBag.ProgramLevels = programLevels;

            ViewBag.SelectedLanguage = studentItem?.ProgramSelectionData?.Language ?? "";
        }

        private void PopulateViewBagForPersonalInformation(StudentApplicationItem? studentItem = null)
        {
            _logger.LogInformation("PopulateViewBagForPersonalInformation called. StudentItem is null: {IsStudentItemNull}", studentItem == null);
            if (studentItem?.PersonalInformationData != null)
            {
                _logger.LogInformation("StudentItem Personal Data: CountryOfBirth='{CountryOfBirth}', Gender='{Gender}'", studentItem.PersonalInformationData.CountryOfBirth, studentItem.PersonalInformationData.Gender);
            }

            var rawCountries = new List<SelectListItem> 
            {
                new SelectListItem { Value = "Afghanistan", Text = "Afghanistan" },
                new SelectListItem { Value = "Albania", Text = "Albania" },
                new SelectListItem { Value = "Algeria", Text = "Algeria" },
                new SelectListItem { Value = "Andorra", Text = "Andorra" },
                new SelectListItem { Value = "Angola", Text = "Angola" },
                new SelectListItem { Value = "Argentina", Text = "Argentina" },
                new SelectListItem { Value = "Armenia", Text = "Armenia" },
                new SelectListItem { Value = "Australia", Text = "Australia" },
                new SelectListItem { Value = "Austria", Text = "Austria" },
                new SelectListItem { Value = "Azerbaijan", Text = "Azerbaijan" },
                new SelectListItem { Value = "Bahamas", Text = "Bahamas" },
                new SelectListItem { Value = "Bahrain", Text = "Bahrain" },
                new SelectListItem { Value = "Bangladesh", Text = "Bangladesh" },
                new SelectListItem { Value = "Belarus", Text = "Belarus" },
                new SelectListItem { Value = "Belgium", Text = "Belgium" },
                new SelectListItem { Value = "Belize", Text = "Belize" },
                new SelectListItem { Value = "Benin", Text = "Benin" },
                new SelectListItem { Value = "Bhutan", Text = "Bhutan" },
                new SelectListItem { Value = "Bolivia", Text = "Bolivia" },
                new SelectListItem { Value = "Bosnia and Herzegovina", Text = "Bosnia and Herzegovina" },
                new SelectListItem { Value = "Botswana", Text = "Botswana" },
                new SelectListItem { Value = "Brazil", Text = "Brazil" },
                new SelectListItem { Value = "Brunei", Text = "Brunei" },
                new SelectListItem { Value = "Bulgaria", Text = "Bulgaria" },
                new SelectListItem { Value = "Burkina Faso", Text = "Burkina Faso" },
                new SelectListItem { Value = "Burundi", Text = "Burundi" },
                new SelectListItem { Value = "Cambodia", Text = "Cambodia" },
                new SelectListItem { Value = "Cameroon", Text = "Cameroon" },
                new SelectListItem { Value = "Canada", Text = "Canada" },
                new SelectListItem { Value = "Central African Republic", Text = "Central African Republic" },
                new SelectListItem { Value = "Chad", Text = "Chad" },
                new SelectListItem { Value = "Chile", Text = "Chile" },
                new SelectListItem { Value = "China", Text = "China" },
                new SelectListItem { Value = "Colombia", Text = "Colombia" },
                new SelectListItem { Value = "Comoros", Text = "Comoros" },
                new SelectListItem { Value = "Congo, Democratic Republic of the", Text = "Congo, Democratic Republic of the" },
                new SelectListItem { Value = "Congo, Republic of the", Text = "Congo, Republic of the" },
                new SelectListItem { Value = "Costa Rica", Text = "Costa Rica" },
                new SelectListItem { Value = "Croatia", Text = "Croatia" },
                new SelectListItem { Value = "Cuba", Text = "Cuba" },
                new SelectListItem { Value = "Cyprus", Text = "Cyprus" },
                new SelectListItem { Value = "Czech Republic", Text = "Czech Republic" },
                new SelectListItem { Value = "Denmark", Text = "Denmark" },
                new SelectListItem { Value = "Djibouti", Text = "Djibouti" },
                new SelectListItem { Value = "Dominican Republic", Text = "Dominican Republic" },
                new SelectListItem { Value = "Ecuador", Text = "Ecuador" },
                new SelectListItem { Value = "Egypt", Text = "Egypt" },
                new SelectListItem { Value = "El Salvador", Text = "El Salvador" },
                new SelectListItem { Value = "Equatorial Guinea", Text = "Equatorial Guinea" },
                new SelectListItem { Value = "Estonia", Text = "Estonia" },
                new SelectListItem { Value = "Ethiopia", Text = "Ethiopia" },
                new SelectListItem { Value = "Fiji", Text = "Fiji" },
                new SelectListItem { Value = "Finland", Text = "Finland" },
                new SelectListItem { Value = "France", Text = "France" },
                new SelectListItem { Value = "Gabon", Text = "Gabon" },
                new SelectListItem { Value = "Gambia", Text = "Gambia" },
                new SelectListItem { Value = "Georgia", Text = "Georgia" },
                new SelectListItem { Value = "Germany", Text = "Germany" },
                new SelectListItem { Value = "Ghana", Text = "Ghana" },
                new SelectListItem { Value = "Greece", Text = "Greece" },
                new SelectListItem { Value = "Guatemala", Text = "Guatemala" },
                new SelectListItem { Value = "Guinea", Text = "Guinea" },
                new SelectListItem { Value = "Guyana", Text = "Guyana" },
                new SelectListItem { Value = "Haiti", Text = "Haiti" },
                new SelectListItem { Value = "Honduras", Text = "Honduras" },
                new SelectListItem { Value = "Hungary", Text = "Hungary" },
                new SelectListItem { Value = "Iceland", Text = "Iceland" },
                new SelectListItem { Value = "India", Text = "India" },
                new SelectListItem { Value = "Indonesia", Text = "Indonesia" },
                new SelectListItem { Value = "Iran", Text = "Iran" },
                new SelectListItem { Value = "Iraq", Text = "Iraq" },
                new SelectListItem { Value = "Ireland", Text = "Ireland" },
                new SelectListItem { Value = "Israel", Text = "Israel" },
                new SelectListItem { Value = "Italy", Text = "Italy" },
                new SelectListItem { Value = "Jamaica", Text = "Jamaica" },
                new SelectListItem { Value = "Japan", Text = "Japan" },
                new SelectListItem { Value = "Jordan", Text = "Jordan" },
                new SelectListItem { Value = "Kazakhstan", Text = "Kazakhstan" },
                new SelectListItem { Value = "Kenya", Text = "Kenya" },
                new SelectListItem { Value = "Kuwait", Text = "Kuwait" },
                new SelectListItem { Value = "Kyrgyzstan", Text = "Kyrgyzstan" },
                new SelectListItem { Value = "Laos", Text = "Laos" },
                new SelectListItem { Value = "Latvia", Text = "Latvia" },
                new SelectListItem { Value = "Lebanon", Text = "Lebanon" },
                new SelectListItem { Value = "Liberia", Text = "Liberia" },
                new SelectListItem { Value = "Libya", Text = "Libya" },
                new SelectListItem { Value = "Liechtenstein", Text = "Liechtenstein" },
                new SelectListItem { Value = "Lithuania", Text = "Lithuania" },
                new SelectListItem { Value = "Luxembourg", Text = "Luxembourg" },
                new SelectListItem { Value = "Madagascar", Text = "Madagascar" },
                new SelectListItem { Value = "Malaysia", Text = "Malaysia" },
                new SelectListItem { Value = "Maldives", Text = "Maldives" },
                new SelectListItem { Value = "Mali", Text = "Mali" },
                new SelectListItem { Value = "Malta", Text = "Malta" },
                new SelectListItem { Value = "Mauritania", Text = "Mauritania" },
                new SelectListItem { Value = "Mauritius", Text = "Mauritius" },
                new SelectListItem { Value = "Mexico", Text = "Mexico" },
                new SelectListItem { Value = "Moldova", Text = "Moldova" },
                new SelectListItem { Value = "Monaco", Text = "Monaco" },
                new SelectListItem { Value = "Mongolia", Text = "Mongolia" },
                new SelectListItem { Value = "Montenegro", Text = "Montenegro" },
                new SelectListItem { Value = "Morocco", Text = "Morocco" },
                new SelectListItem { Value = "Mozambique", Text = "Mozambique" },
                new SelectListItem { Value = "Myanmar", Text = "Myanmar (Burma)" },
                new SelectListItem { Value = "Namibia", Text = "Namibia" },
                new SelectListItem { Value = "Nepal", Text = "Nepal" },
                new SelectListItem { Value = "Netherlands", Text = "Netherlands" },
                new SelectListItem { Value = "New Zealand", Text = "New Zealand" },
                new SelectListItem { Value = "Nicaragua", Text = "Nicaragua" },
                new SelectListItem { Value = "Niger", Text = "Niger" },
                new SelectListItem { Value = "Nigeria", Text = "Nigeria" },
                new SelectListItem { Value = "North Korea", Text = "North Korea" },
                new SelectListItem { Value = "North Macedonia", Text = "North Macedonia" },
                new SelectListItem { Value = "Norway", Text = "Norway" },
                new SelectListItem { Value = "Oman", Text = "Oman" },
                new SelectListItem { Value = "Pakistan", Text = "Pakistan" },
                new SelectListItem { Value = "Palestine", Text = "Palestine" },
                new SelectListItem { Value = "Panama", Text = "Panama" },
                new SelectListItem { Value = "Paraguay", Text = "Paraguay" },
                new SelectListItem { Value = "Peru", Text = "Peru" },
                new SelectListItem { Value = "Philippines", Text = "Philippines" },
                new SelectListItem { Value = "Poland", Text = "Poland" },
                new SelectListItem { Value = "Portugal", Text = "Portugal" },
                new SelectListItem { Value = "Qatar", Text = "Qatar" },
                new SelectListItem { Value = "Romania", Text = "Romania" },
                new SelectListItem { Value = "Russia", Text = "Russia" },
                new SelectListItem { Value = "Rwanda", Text = "Rwanda" },
                new SelectListItem { Value = "Saudi Arabia", Text = "Saudi Arabia" },
                new SelectListItem { Value = "Senegal", Text = "Senegal" },
                new SelectListItem { Value = "Serbia", Text = "Serbia" },
                new SelectListItem { Value = "Sierra Leone", Text = "Sierra Leone" },
                new SelectListItem { Value = "Singapore", Text = "Singapore" },
                new SelectListItem { Value = "Slovakia", Text = "Slovakia" },
                new SelectListItem { Value = "Slovenia", Text = "Slovenia" },
                new SelectListItem { Value = "Somalia", Text = "Somalia" },
                new SelectListItem { Value = "South Africa", Text = "South Africa" },
                new SelectListItem { Value = "South Korea", Text = "South Korea" },
                new SelectListItem { Value = "Spain", Text = "Spain" },
                new SelectListItem { Value = "Sri Lanka", Text = "Sri Lanka" },
                new SelectListItem { Value = "Sudan", Text = "Sudan" },
                new SelectListItem { Value = "Sweden", Text = "Sweden" },
                new SelectListItem { Value = "Switzerland", Text = "Switzerland" },
                new SelectListItem { Value = "Syria", Text = "Syria" },
                new SelectListItem { Value = "Taiwan", Text = "Taiwan" },
                new SelectListItem { Value = "Tanzania", Text = "Tanzania" },
                new SelectListItem { Value = "Thailand", Text = "Thailand" },
                new SelectListItem { Value = "Togo", Text = "Togo" },
                new SelectListItem { Value = "Tunisia", Text = "Tunisia" },
                new SelectListItem { Value = "Turkey", Text = "Turkey" },
                new SelectListItem { Value = "Turkmenistan", Text = "Turkmenistan" },
                new SelectListItem { Value = "Uganda", Text = "Uganda" },
                new SelectListItem { Value = "Ukraine", Text = "Ukraine" },
                new SelectListItem { Value = "United Arab Emirates", Text = "United Arab Emirates" },
                new SelectListItem { Value = "United Kingdom", Text = "United Kingdom" },
                new SelectListItem { Value = "USA", Text = "United States" }, 
                new SelectListItem { Value = "Uruguay", Text = "Uruguay" },
                new SelectListItem { Value = "Uzbekistan", Text = "Uzbekistan" },
                new SelectListItem { Value = "Venezuela", Text = "Venezuela" },
                new SelectListItem { Value = "Vietnam", Text = "Vietnam" },
                new SelectListItem { Value = "Yemen", Text = "Yemen" },
                new SelectListItem { Value = "Zambia", Text = "Zambia" },
                new SelectListItem { Value = "Zimbabwe", Text = "Zimbabwe" }
            };

            
            var distinctCountries = rawCountries
                .GroupBy(c => c.Text)
                .Select(g => g.First())
                .OrderBy(c => c.Text)
                .ToList();

           
            var finalCountries = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "Select Country", Disabled = true, Selected = string.IsNullOrEmpty(studentItem?.PersonalInformationData?.CountryOfBirth) }
            };
            finalCountries.AddRange(distinctCountries);

            ViewBag.Countries = finalCountries;
            _logger.LogInformation("ViewBag.Countries populated. Count: {Count}. First few: {FirstCountries}", finalCountries.Count, string.Join(", ", finalCountries.Take(5).Select(c => c.Text)));

            var genders = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "Select Gender", Disabled = true, Selected = string.IsNullOrEmpty(studentItem?.PersonalInformationData?.Gender) },
                new SelectListItem { Value = "Male", Text = "Male" },
                new SelectListItem { Value = "Female", Text = "Female" },
                new SelectListItem { Value = "Other", Text = "Other" }
            };
            ViewBag.Genders = genders;
            _logger.LogInformation("ViewBag.Genders populated. Count: {Count}. First few: {FirstGenders}", genders.Count, string.Join(", ", genders.Take(3).Select(g => g.Text)));
        }

        private void PopulateViewBagForEducationalInformation(StudentApplicationItem? studentItem = null)
        {
            _logger.LogInformation("PopulateViewBagForEducationalInformation called. StudentItem is null: {IsStudentItemNull}", studentItem == null);
            if (studentItem?.EducationalInformationData != null)
            {
                _logger.LogInformation("StudentItem Educational Data: Country='{Country}', Degree='{Degree}', Proficiency='{Proficiency}'", 
                    studentItem.EducationalInformationData.Country, studentItem.EducationalInformationData.Degree, studentItem.EducationalInformationData.LanguageProficiency);
            }

            if (ViewBag.Countries == null) 
            {
                _logger.LogWarning("ViewBag.Countries was null in PopulateViewBagForEducationalInformation. Populating countries as a fallback.");
                var rawCountriesForEducation = new List<SelectListItem> 
                {
                    new SelectListItem { Value = "", Text = "Select Country", Disabled = true, Selected = string.IsNullOrEmpty(studentItem?.EducationalInformationData?.Country) }
                };
                var commonCountries = new List<string> { "Turkey", "United States", "United Kingdom", "Germany", "Canada", "Azerbaijan" }; 
                rawCountriesForEducation.AddRange(commonCountries.Select(c => new SelectListItem { Value = c, Text = c, Selected = c == studentItem?.EducationalInformationData?.Country }));
                rawCountriesForEducation.Add(new SelectListItem { Value = "Other", Text = "Other (Specify Below)", Selected = !string.IsNullOrEmpty(studentItem?.EducationalInformationData?.Country) && !commonCountries.Contains(studentItem?.EducationalInformationData?.Country ?? "") });
                ViewBag.Countries = rawCountriesForEducation;
            }

            ViewBag.DegreeOptions = new List<SelectListItem>
            {
               
                new SelectListItem { Value = "Bachelor Degree", Text = "Bachelor Degree" },
                new SelectListItem { Value = "Master Degree", Text = "Master Degree" },
                new SelectListItem { Value = "PhD Degree", Text = "PhD Degree" }
            };
            _logger.LogInformation("ViewBag.DegreeOptions populated. Count: {Count}", (ViewBag.DegreeOptions as List<SelectListItem>)?.Count ?? 0);

            var proficiencies = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "Select Language Proficiency", Disabled = true, Selected = string.IsNullOrEmpty(studentItem?.EducationalInformationData?.LanguageProficiency) },
                new SelectListItem { Value = "Beginner", Text = "Beginner (A1-A2)" },
                new SelectListItem { Value = "Intermediate", Text = "Intermediate (B1-B2)" },
                new SelectListItem { Value = "Advanced", Text = "Advanced (C1-C2)" },
                new SelectListItem { Value = "Native", Text = "Native Speaker" }
            };
            ViewBag.LanguageProficiencies = proficiencies;
            _logger.LogInformation("ViewBag.LanguageProficiencies populated. Count: {Count}", proficiencies.Count);
        }

        private void PopulateViewBagForDocuments(StudentApplicationItem studentItem)
        {
            var requiredDocTypes = new List<string>();
            var optionalDocTypes = new List<string>();

            // Default required documents
            requiredDocTypes.Add("Diploma");
            requiredDocTypes.Add("Transcript");

            
            if (studentItem.PersonalInformationData != null)
            {
                bool requiresPassport = (studentItem.PersonalInformationData.CountryOfBirth?.ToLower() != "turkey") && 
                                        (studentItem.PersonalInformationData.Citizenship?.ToLower() != "turkey");
                if (requiresPassport)
                {
                    requiredDocTypes.Add("Passport");
                }
            }

            
            if (studentItem.ProgramSelectionData != null)
            {
                
                bool isConsideredTurkishForALES = false;
                if (studentItem.PersonalInformationData != null)
                {
                    bool isCitizenshipTurkey = "turkey".Equals(studentItem.PersonalInformationData.Citizenship, StringComparison.OrdinalIgnoreCase);
                    bool isCountryOfBirthTurkey = "turkey".Equals(studentItem.PersonalInformationData.CountryOfBirth, StringComparison.OrdinalIgnoreCase);
                    isConsideredTurkishForALES = isCitizenshipTurkey || isCountryOfBirthTurkey;

                    if (!isCitizenshipTurkey && !isCountryOfBirthTurkey)
                    {
                        _logger.LogInformation($"Student {studentItem.StudentName} (TempID: {studentItem.TemporaryId}): Neither Citizenship ('{studentItem.PersonalInformationData.Citizenship}') nor Country of Birth ('{studentItem.PersonalInformationData.CountryOfBirth}') is Turkey. ALES will not be required based on this.");
                    }
                    else
                    {
                         _logger.LogInformation($"Student {studentItem.StudentName} (TempID: {studentItem.TemporaryId}): Citizenship ('{studentItem.PersonalInformationData.Citizenship}') or Country of Birth ('{studentItem.PersonalInformationData.CountryOfBirth}') is Turkey. ALES will be required.");
                    }
                }
                else
                {
                    _logger.LogWarning($"PersonalInformationData is null for student {studentItem.StudentName} (TempID: {studentItem.TemporaryId}) when determining ALES requirement. Assuming non-Turkish for ALES unless other conditions trigger it.");
                }

                if (isConsideredTurkishForALES)
                {
                    requiredDocTypes.Add("ALES Result");
                }

                bool isTurkishProgram = "Turkish".Equals(studentItem.ProgramSelectionData.Language, StringComparison.OrdinalIgnoreCase);
                string programLevel = studentItem.ProgramSelectionData.ProgramLevel;

                
                if (("MasterThesis".Equals(programLevel, StringComparison.OrdinalIgnoreCase) || "PhD".Equals(programLevel, StringComparison.OrdinalIgnoreCase)) && !isTurkishProgram)
                {
                    requiredDocTypes.Add("Language Exam Result");
                }

              
                if ("PhD".Equals(programLevel, StringComparison.OrdinalIgnoreCase))
                {
                    requiredDocTypes.Add("Master Thesis Cover Page");
                }
            }

           
            optionalDocTypes.Add("Reference Letter 1");
            optionalDocTypes.Add("Reference Letter 2");
           

            ViewBag.AllPossibleDocumentTypes = new List<string> {
                "Passport", "Diploma", "Transcript", "ALES Result", "Language Exam Result",
                "Master Thesis Cover Page", "Reference Letter 1", "Reference Letter 2", "Other"
            }.Distinct().ToList();

            ViewBag.RequiredDocTypes = requiredDocTypes.Distinct().ToList();
            ViewBag.OptionalDocTypes = optionalDocTypes.Distinct().Except(requiredDocTypes).ToList(); 
            
       
            ViewBag.UploadedTemporaryDocuments = studentItem.TemporaryDocuments ?? new List<StudentTemporaryDocument>();
            
            var uploadedTempDocTypes = studentItem.TemporaryDocuments?.Select(d => d.DocumentType).ToList() ?? new List<string>();
            ViewBag.MissingRequiredDocTypes = requiredDocTypes.Except(uploadedTempDocTypes).ToList();
        }

        public IActionResult Index()
        {
            _logger.LogInformation("BulkApplication Index page requested.");
            var model = GetBulkApplicationFromSession();
            _logger.LogInformation("CurrentEditingStudentTemporaryId: {TempId}", model.CurrentEditingStudentTemporaryId);

            
            PopulateGeneralViewBagsForNewApplication();

            if (model.CurrentEditingStudentTemporaryId.HasValue)
            {
                var currentStudent = model.StudentApplications.FirstOrDefault(s => s.TemporaryId == model.CurrentEditingStudentTemporaryId.Value);
                if (currentStudent != null)
                {
                    _logger.LogInformation("Current student for editing: {StudentName}, Target Step from Session: {CurrentStep}", currentStudent.StudentName, currentStudent.CurrentStep);
                   
                    switch (currentStudent.CurrentStep)
                    {
                        case 1: 
                            _logger.LogInformation("Re-Populating ViewBag for Program Selection (Step 1) with student data.");
                            PopulateViewBagForProgramSelection(currentStudent);
                            break;
                        case 2: 
                            _logger.LogInformation("Re-Populating ViewBag for Personal Information (Step 2) with student data.");
                            PopulateViewBagForPersonalInformation(currentStudent); 
                            break;
                        case 3: 
                            _logger.LogInformation("Re-Populating ViewBag for Educational Information (Step 3) with student data.");
                            PopulateViewBagForEducationalInformation(currentStudent);
                            break;
                        case 4: 
                            _logger.LogInformation("Re-Populating ViewBag for Documents (Step 4) with student data.");
                            PopulateViewBagForDocuments(currentStudent); 
                            break;
                    }
                }
                else
                {
                    _logger.LogWarning($"CurrentEditingStudentTemporaryId {model.CurrentEditingStudentTemporaryId.Value} was set, but student not found in list.");
                }
            }
            else
            {
                _logger.LogInformation("No specific student is being edited.");
            }

            
            if (TempData["SuccessMessage"] != null) ViewBag.SuccessMessage = TempData["SuccessMessage"];
            if (TempData["ErrorMessage"] != null) ViewBag.ErrorMessage = TempData["ErrorMessage"];
            if (TempData["WarningMessage"] != null) ViewBag.WarningMessage = TempData["WarningMessage"];

            return View(model);
        }

        
        private void PopulateGeneralViewBagsForNewApplication()
        {
            _logger.LogInformation("PopulateGeneralViewBagsForNewApplication called.");
            PopulateViewBagForProgramSelection(); 
            PopulateViewBagForPersonalInformation();
            PopulateViewBagForEducationalInformation();
        
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddStudent()
        {
            var model = GetBulkApplicationFromSession();
            var newStudent = new StudentApplicationItem();
            
            int counter = 1;
            string baseName = "New Student Application";
            string currentName = $"{baseName} {counter}"; 
            
            
            var existingNumberedStudents = model.StudentApplications
                .Where(s => s.StudentName.StartsWith(baseName + " "))
                .Select(s => s.StudentName.Replace(baseName + " ", ""))
                .Select(s_num => int.TryParse(s_num, out int num) ? num : 0)
                .ToList();
            
            if (existingNumberedStudents.Any())
            {
                counter = existingNumberedStudents.Max() + 1;
            }
            currentName = $"{baseName} {counter}";

            
            while(model.StudentApplications.Any(s => s.StudentName == currentName))
            {
                currentName = $"{baseName} {++counter}";
            }
            newStudent.StudentName = currentName;
            newStudent.CurrentStep = 1; 
            
            model.StudentApplications.Add(newStudent);
            model.CurrentEditingStudentTemporaryId = newStudent.TemporaryId; 

            SaveBulkApplicationToSession(model);
            _logger.LogInformation($"Added new student '{@newStudent.StudentName}' with TempID: {newStudent.TemporaryId}. Total students: {model.StudentApplications.Count}");

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult EditStudentApplication(Guid tempId, int? targetStep)
        {
            if (tempId == Guid.Empty)
            {
                _logger.LogWarning("EditStudentApplication called with empty Guid.");
                TempData["ErrorMessage"] = "Invalid student identifier.";
                return RedirectToAction("Index");
            }

            var model = GetBulkApplicationFromSession();
            var studentToEdit = model.StudentApplications.FirstOrDefault(s => s.TemporaryId == tempId);

            if (studentToEdit == null)
            {
                _logger.LogWarning($"Student with TempID: {tempId} not found in session for editing.");
                TempData["ErrorMessage"] = "Student application not found.";
                model.CurrentEditingStudentTemporaryId = null; 
            }
            else
            {
                model.CurrentEditingStudentTemporaryId = tempId;
                if (targetStep.HasValue && targetStep.Value > 0 && targetStep.Value < studentToEdit.CurrentStep) 
                {
                    studentToEdit.CurrentStep = targetStep.Value;
                    _logger.LogInformation($"Set student '{studentToEdit.StudentName}' (TempID: {tempId}) to step {targetStep.Value} for editing.");
                }
                else
                {
                    _logger.LogInformation($"Set student '{studentToEdit.StudentName}' (TempID: {tempId}) as current for editing (current step: {studentToEdit.CurrentStep}).");
                }
            }
            
            SaveBulkApplicationToSession(model);
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailablePrograms(string language, string level)
        {
            if (string.IsNullOrEmpty(language) || string.IsNullOrEmpty(level))
            {
                return Json(new { message = "Please select both language and program level." });
            }

            var now = DateTime.UtcNow.Date;
            var openPeriod = await _context.ApplicationPeriods
                                        .FirstOrDefaultAsync(p => p.IsOpen && 
                                                                p.StartDate.Date <= now && 
                                                                p.EndDate.Date >= now);

            if (openPeriod == null)
            {
                return Json(new { message = "There is no active application period for the selected criteria or in general. Please check application period settings." });
            }

            try
            {
                var programs = await _context.Programs
                    .Where(p => p.Language == language && p.Level == level) 
                    .Select(p => new 
                    {
                        Id = p.ProgramId,
                        Name = p.Name,
                        NameEn = p.NameEn,
                        DisplayName = (language == "English" && !string.IsNullOrEmpty(p.NameEn)) ? p.NameEn : p.Name,
                        p.FacultyInstitute 
                    })
                    .OrderBy(p=> p.DisplayName)
                    .ToListAsync();

                if (!programs.Any())
                {
                    return Json(new { message = "No programs found for the selected criteria within the active application period." });
                }
                return Json(programs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching available programs for language {Language} and level {Level}", language, level);
                return Json(new { message = "An error occurred while fetching programs. Please try again." });
            }
        }
         
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitAllApplications()
        {
            var model = GetBulkApplicationFromSession();
            if (model.StudentApplications == null || !model.StudentApplications.Any())
            {
                TempData["ErrorMessage"] = "No applications to submit.";
                return RedirectToAction("Index");
            }

            _logger.LogInformation($"Attempting to submit {model.StudentApplications.Count} applications.");
            
            int successfulSubmissions = 0;
            int failedSubmissions = 0;
            List<string> errorMessages = new List<string>();
            var currentAgencyUserId = _userManager.GetUserId(User); 

            foreach (var studentAppItem in model.StudentApplications)
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        
                        var user = await _userManager.FindByEmailAsync(studentAppItem.PersonalInformationData.Email);
                        bool newUserCreated = false;

                        if (user == null)
                        {
                            user = new ApplicationUser
                            {
                                UserName = studentAppItem.PersonalInformationData.Email,
                                Email = studentAppItem.PersonalInformationData.Email,
                                FirstName = studentAppItem.PersonalInformationData.FirstName,
                                LastName = studentAppItem.PersonalInformationData.LastName,
                                EmailConfirmed = false, 
                                RegistrationDate = DateTime.UtcNow,
                                Role = "Candidate",
                                IsApproved = true, 
                                IsActive = true,
                                TrackingNumber = Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper()
                            };
                            var createUserResult = await _userManager.CreateAsync(user);
                            if (!createUserResult.Succeeded)
                            {
                                throw new Exception($"User creation failed for {user.Email}: {string.Join(", ", createUserResult.Errors.Select(e => e.Description))}");
                            }
                            newUserCreated = true;
                            _logger.LogInformation($"New user created: {user.Email}");
                        }
                        else
                        {
                            _logger.LogInformation($"Existing user found: {user.Email}. Attaching application to this user.");
                        }

                        
                        var programSelection = new ProgramSelection
                        {
                            UserId = user.Id, 
                            TrackingNumber = user.TrackingNumber,
                            SubmittedByAgencyUserId = currentAgencyUserId,
                            Language = studentAppItem.ProgramSelectionData.Language,
                            ProgramLevel = studentAppItem.ProgramSelectionData.ProgramLevel,
                            FacultyInstitute = studentAppItem.ProgramSelectionData.FacultyInstitute,
                            ApplicationStatus = "Submitted", 
                            CreatedAt = DateTime.UtcNow,
                            ApplicationPeriodId = studentAppItem.ProgramSelectionData.ApplicationPeriodId,
                            ProgramChoices = studentAppItem.ProgramSelectionData.ProgramChoices.Select(c => new ProgramChoice
                            {
                                ProgramName = c.ProgramName,
                                DepartmentId = c.DepartmentId,
                                Choice = c.Choice
                            }).ToList()
                        };
                        await _context.ProgramSelections.AddAsync(programSelection);
                        await _context.SaveChangesAsync(); 

                       
                        var personalInfo = studentAppItem.PersonalInformationData;
                        personalInfo.ProgramSelectionId = programSelection.Id; 
                        _context.Entry(personalInfo).State = personalInfo.Id == 0 ? EntityState.Added : EntityState.Modified;
                        if(personalInfo.Id == 0) _context.PersonalInformations.Add(personalInfo);

                        
                        var educationalInfo = studentAppItem.EducationalInformationData;
                        educationalInfo.ProgramSelectionId = programSelection.Id;
                        _context.Entry(educationalInfo).State = educationalInfo.Id == 0 ? EntityState.Added : EntityState.Modified;
                        if(educationalInfo.Id == 0) _context.EducationalInformations.Add(educationalInfo);

                        await _context.SaveChangesAsync(); 

                       
                        if (studentAppItem.TemporaryDocuments != null && studentAppItem.TemporaryDocuments.Any())
                        {
                            foreach (var tempDoc in studentAppItem.TemporaryDocuments)
                            {
                                if (string.IsNullOrEmpty(tempDoc.TemporaryFilePath) || !System.IO.File.Exists(tempDoc.TemporaryFilePath))
                                {
                                    _logger.LogWarning($"Temporary file path for {tempDoc.FileName} (Type: {tempDoc.DocumentType}) is missing or file does not exist for student {studentAppItem.PersonalInformationData.Email}. Path: '{tempDoc.TemporaryFilePath}'. Skipping this document.");
                                    errorMessages.Add($"Document '{tempDoc.FileName}' for {studentAppItem.PersonalInformationData.Email} could not be processed: temporary file not found.");
                                 
                                    continue; 
                                }

                                byte[] fileBytes;
                                try
                                {
                                    fileBytes = await System.IO.File.ReadAllBytesAsync(tempDoc.TemporaryFilePath);
                                }
                                catch (Exception readEx)
                                {
                                    _logger.LogError(readEx, $"Error reading temporary file {tempDoc.TemporaryFilePath} for document {tempDoc.FileName} of student {studentAppItem.PersonalInformationData.Email}.");
                                    errorMessages.Add($"Could not read file for document '{tempDoc.FileName}' of student {studentAppItem.PersonalInformationData.Email}.");
                                    continue;
                                }

                                var document = new Document
                                {
                                    ProgramSelectionId = programSelection.Id, 
                                    DocumentType = tempDoc.DocumentType,
                                    OriginalFileName = tempDoc.FileName, 
                                    FileName = Guid.NewGuid().ToString() + Path.GetExtension(tempDoc.FileName), 
                                    ContentType = tempDoc.ContentType,
                                    FileSize = tempDoc.FileSize,
                                    FileContent = fileBytes,
                                    UploadDate = DateTime.UtcNow
                                };
                                await _context.Documents.AddAsync(document);
                                _logger.LogInformation($"Document {document.OriginalFileName} (Type: {document.DocumentType}) for user {user.Email} prepared with content.");
                            }
                            await _context.SaveChangesAsync(); 

                           
                            var studentSpecificTempPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "temp_uploads", studentAppItem.TemporaryId.ToString());
                            if (Directory.Exists(studentSpecificTempPath))
                            {
                                try
                                {
                                    Directory.Delete(studentSpecificTempPath, true); 
                                    _logger.LogInformation($"Successfully deleted temporary upload directory: {studentSpecificTempPath} for student {studentAppItem.PersonalInformationData.Email}");
                                }
                                catch (IOException ioEx)
                                {
                                    _logger.LogError(ioEx, $"IO Error deleting temporary upload directory {studentSpecificTempPath} for student {studentAppItem.PersonalInformationData.Email}. Some files might be locked.");
                                   
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, $"General Error deleting temporary upload directory {studentSpecificTempPath} for student {studentAppItem.PersonalInformationData.Email}.");
                                }
                            }
                        }

                        
                        if (newUserCreated)
                        {
                            var emailConfirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                            var confirmationLink = $"https://{Request.Host}/Account/ConfirmEmail?userId={user.Id}&code={System.Net.WebUtility.UrlEncode(emailConfirmationToken)}";
                            _logger.LogInformation($"SIMULATING sending account activation email to {user.Email} with link: {confirmationLink}");
                        }
                        else
                        {
                            _logger.LogInformation($"SIMULATING sending application update email to existing user {user.Email}.");
                        }

                        await transaction.CommitAsync();
                        successfulSubmissions++;
                        _logger.LogInformation($"Successfully submitted application for {studentAppItem.PersonalInformationData.Email}");
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        failedSubmissions++;
                        _logger.LogError(ex, $"Failed to submit application for {studentAppItem.PersonalInformationData.Email}");
                        errorMessages.Add($"Error for {studentAppItem.PersonalInformationData.Email}: {ex.Message}");
                    }
                } 
            } 

            _httpContextAccessor.HttpContext.Session.Remove(BulkApplicationSessionKey);

            if (failedSubmissions > 0)
            {
                TempData["ErrorMessage"] = $"{failedSubmissions} application(s) could not be submitted. Errors: {string.Join("; ", errorMessages)}";
            }
            if (successfulSubmissions > 0)
            {
                TempData["SuccessMessage"] = $"{successfulSubmissions} application(s) submitted successfully.";
            }
            if(successfulSubmissions == 0 && failedSubmissions == 0) 
            {
                TempData["InfoMessage"] = "No applications were processed.";
            }

            return RedirectToAction("Index", "Home", new { area = "Agency" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveProgramSelection(StudentApplicationItem studentFormData)
        {
            if (studentFormData == null || studentFormData.TemporaryId == Guid.Empty)
            {
                TempData["ErrorMessage"] = "Invalid student data submitted.";
                return RedirectToAction("Index");
            }
            
            
            var now = DateTime.UtcNow.Date;
            var openPeriod = await _context.ApplicationPeriods
                                        .FirstOrDefaultAsync(p => p.IsOpen && 
                                                                p.StartDate.Date <= now && 
                                                                p.EndDate.Date >= now);
            if (openPeriod == null)
            {
                TempData["ErrorMessage"] = "Cannot save program selection as there is no active application period.";
                
                
                var currentModel = GetBulkApplicationFromSession();
                currentModel.CurrentEditingStudentTemporaryId = studentFormData.TemporaryId;

                SaveBulkApplicationToSession(currentModel); 
                return RedirectToAction("Index");
            }

            var model = GetBulkApplicationFromSession();
            var studentToUpdate = model.StudentApplications.FirstOrDefault(s => s.TemporaryId == studentFormData.TemporaryId);

            if (studentToUpdate == null)
            {
                TempData["ErrorMessage"] = "Could not find the student application to update.";
                return RedirectToAction("Index");
            }

            studentToUpdate.ProgramSelectionData.Language = studentFormData.ProgramSelectionData.Language;
            studentToUpdate.ProgramSelectionData.ProgramLevel = studentFormData.ProgramSelectionData.ProgramLevel;
            studentToUpdate.ProgramSelectionData.FacultyInstitute = studentFormData.ProgramSelectionData.FacultyInstitute;
            studentToUpdate.ProgramSelectionData.ApplicationStatus = "Draft";
            studentToUpdate.ProgramSelectionData.ApplicationPeriodId = openPeriod.Id; 

            studentToUpdate.ProgramSelectionData.ProgramChoices.Clear();
            if (studentFormData.ProgramSelectionData.ProgramChoices != null)
            {
                foreach (var choice in studentFormData.ProgramSelectionData.ProgramChoices.OrderBy(c => c.Choice))
                {
                    if (!string.IsNullOrWhiteSpace(choice.ProgramName))
                    {
                        var programNameToSearch = choice.ProgramName; 

                        var programDetail = await _context.Programs
                                                    .Include(p => p.Department)
                                                    .FirstOrDefaultAsync(p => (p.Name == programNameToSearch || p.NameEn == programNameToSearch) &&
                                                                            p.Language == studentToUpdate.ProgramSelectionData.Language && 
                                                                            p.Level == studentToUpdate.ProgramSelectionData.ProgramLevel);

                        if (programDetail == null && studentToUpdate.ProgramSelectionData.Language == "English")
                        {
                           
                           programDetail = await _context.Programs
                                                    .Include(p => p.Department)
                                                    .FirstOrDefaultAsync(p => p.Name == programNameToSearch &&
                                                                            p.Language == studentToUpdate.ProgramSelectionData.Language &&
                                                                            p.Level == studentToUpdate.ProgramSelectionData.ProgramLevel);
                        }
                         if (programDetail == null && studentToUpdate.ProgramSelectionData.Language == "Turkish") 
                        {
                            
                           programDetail = await _context.Programs
                                                    .Include(p => p.Department)
                                                    .FirstOrDefaultAsync(p => p.NameEn == programNameToSearch &&
                                                                            p.Language == studentToUpdate.ProgramSelectionData.Language &&
                                                                            p.Level == studentToUpdate.ProgramSelectionData.ProgramLevel);
                        }


                        if (programDetail != null)
                        {
                            string nameToSave = (studentToUpdate.ProgramSelectionData.Language == "English" && !string.IsNullOrEmpty(programDetail.NameEn))
                                                ? programDetail.NameEn
                                                : programDetail.Name;

                            studentToUpdate.ProgramSelectionData.ProgramChoices.Add(new ProgramChoice
                            {
                                ProgramName = nameToSave, 
                                DepartmentId = programDetail.DepartmentId,
                                Choice = choice.Choice,
                                ProgramSelection = studentToUpdate.ProgramSelectionData
                            });
                        }
                    }
                }
            }
            
            if (!studentToUpdate.ProgramSelectionData.ProgramChoices.Any())
            {
                TempData["ErrorMessage"] = "At least one valid program must be selected.";
                model.CurrentEditingStudentTemporaryId = studentToUpdate.TemporaryId;
                SaveBulkApplicationToSession(model);
                return RedirectToAction("Index");
            }

            studentToUpdate.CurrentStep = 2; 
            _logger.LogInformation($"Saved Program Selection for student '{studentToUpdate.StudentName}' (TempID: {studentToUpdate.TemporaryId}) for ApplicationPeriodID {openPeriod.Id}. Current step: {studentToUpdate.CurrentStep}");

            model.CurrentEditingStudentTemporaryId = studentToUpdate.TemporaryId; 
            SaveBulkApplicationToSession(model);

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SavePersonalInformation(StudentApplicationItem studentFormData)
        {
            if (studentFormData == null || studentFormData.TemporaryId == Guid.Empty)
            {
                TempData["ErrorMessage"] = "Invalid student data submitted for Personal Information.";
                return RedirectToAction("Index");
            }

            var model = GetBulkApplicationFromSession();
            var studentToUpdate = model.StudentApplications.FirstOrDefault(s => s.TemporaryId == studentFormData.TemporaryId);

            if (studentToUpdate == null)
            {
                TempData["ErrorMessage"] = "Could not find the student application to update Personal Information.";
                return RedirectToAction("Index");
            }

            
            studentToUpdate.PersonalInformationData = studentFormData.PersonalInformationData;
            
            
            if (string.IsNullOrWhiteSpace(studentToUpdate.PersonalInformationData.FirstName) ||
                string.IsNullOrWhiteSpace(studentToUpdate.PersonalInformationData.LastName) ||
                studentToUpdate.PersonalInformationData.DateOfBirth == default(DateTime) || studentToUpdate.PersonalInformationData.DateOfBirth == null || 
                string.IsNullOrWhiteSpace(studentToUpdate.PersonalInformationData.CountryOfBirth) ||
                string.IsNullOrWhiteSpace(studentToUpdate.PersonalInformationData.Citizenship) ||
                string.IsNullOrWhiteSpace(studentToUpdate.PersonalInformationData.CountryOfResidence) ||
                string.IsNullOrWhiteSpace(studentToUpdate.PersonalInformationData.Email) || 
                string.IsNullOrWhiteSpace(studentToUpdate.PersonalInformationData.Gender))
            {
                TempData["ErrorMessage"] = "Please fill all required personal information fields (First Name, Last Name, DOB, Country of Birth, Citizenship, Country of Residence, Email, Gender).";
                model.CurrentEditingStudentTemporaryId = studentToUpdate.TemporaryId; 
                SaveBulkApplicationToSession(model);
                return RedirectToAction("Index");
            }

            studentToUpdate.CurrentStep = 3; 
            _logger.LogInformation($"Saved Personal Information for student '{studentToUpdate.StudentName}' (TempID: {studentToUpdate.TemporaryId}). Current step: {studentToUpdate.CurrentStep}");

            model.CurrentEditingStudentTemporaryId = studentToUpdate.TemporaryId; 
            SaveBulkApplicationToSession(model);

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveEducationalInformation(StudentApplicationItem studentFormData)
        {
            if (studentFormData == null || studentFormData.TemporaryId == Guid.Empty)
            {
                TempData["ErrorMessage"] = "Invalid student data submitted for Educational Information.";
                return RedirectToAction("Index");
            }

            var model = GetBulkApplicationFromSession();
            var studentToUpdate = model.StudentApplications.FirstOrDefault(s => s.TemporaryId == studentFormData.TemporaryId);

            if (studentToUpdate == null)
            {
                TempData["ErrorMessage"] = "Could not find the student application to update Educational Information.";
                return RedirectToAction("Index");
            }

            studentToUpdate.EducationalInformationData = studentFormData.EducationalInformationData;
            
        
            if (string.IsNullOrWhiteSpace(studentToUpdate.EducationalInformationData.SchoolName) ||
                studentToUpdate.EducationalInformationData.GraduationYear == 0 ||
                string.IsNullOrWhiteSpace(studentToUpdate.EducationalInformationData.Degree) ||
                string.IsNullOrWhiteSpace(studentToUpdate.EducationalInformationData.Major) ||
                studentToUpdate.EducationalInformationData.GPA == 0 ||
                string.IsNullOrWhiteSpace(studentToUpdate.EducationalInformationData.Country) ||
                string.IsNullOrWhiteSpace(studentToUpdate.EducationalInformationData.City) ||
                string.IsNullOrWhiteSpace(studentToUpdate.EducationalInformationData.LanguageProficiency) ||
                studentToUpdate.EducationalInformationData.LanguageExamScore == 0)
            {
                TempData["ErrorMessage"] = "Please fill all required educational information fields (School Name, Graduation Year, Degree, Major, GPA, Country, City, Language Proficiency, Language Exam Score).";
                model.CurrentEditingStudentTemporaryId = studentToUpdate.TemporaryId; 
                SaveBulkApplicationToSession(model);
                return RedirectToAction("Index");
            }

            studentToUpdate.CurrentStep = 4; 
            _logger.LogInformation($"Saved Educational Information for student '{studentToUpdate.StudentName}' (TempID: {studentToUpdate.TemporaryId}). Current step: {studentToUpdate.CurrentStep}");

            model.CurrentEditingStudentTemporaryId = studentToUpdate.TemporaryId; 
            SaveBulkApplicationToSession(model);

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveDocumentsStep(StudentApplicationItem studentFormData)
        {
            if (studentFormData == null || studentFormData.TemporaryId == Guid.Empty)
            {
                TempData["ErrorMessage"] = "Invalid student data submitted for Documents step.";
                return RedirectToAction("Index");
            }

            var model = GetBulkApplicationFromSession();
            var studentToUpdate = model.StudentApplications.FirstOrDefault(s => s.TemporaryId == studentFormData.TemporaryId);

            if (studentToUpdate == null)
            {
                TempData["ErrorMessage"] = "Could not find the student application to update Documents.";
                return RedirectToAction("Index");
            }

           
            studentToUpdate.TemporaryDocuments = studentFormData.TemporaryDocuments ?? new List<StudentTemporaryDocument>();
            _logger.LogInformation($"Updated TemporaryDocuments for student '{studentToUpdate.StudentName}'. Count: {studentToUpdate.TemporaryDocuments.Count}");

           
            if (studentToUpdate.TemporaryDocuments.Any())
            {
                var firstDoc = studentToUpdate.TemporaryDocuments.First();
                _logger.LogInformation($"First temp doc details on save for student '{studentToUpdate.StudentName}': Path='{firstDoc.TemporaryFilePath}', Type='{firstDoc.ContentType}', Size='{firstDoc.FileSize}'");
            }

            studentToUpdate.CurrentStep = 5; 
            _logger.LogInformation($"Saved Documents step for student '{studentToUpdate.StudentName}' (TempID: {studentToUpdate.TemporaryId}). Current step: {studentToUpdate.CurrentStep}");

            model.CurrentEditingStudentTemporaryId = studentToUpdate.TemporaryId; 
            SaveBulkApplicationToSession(model);

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PreviousToDocuments()
        {
            var model = GetBulkApplicationFromSession();

            if (model.CurrentEditingStudentTemporaryId.HasValue)
            {
                var studentToEdit = model.StudentApplications.FirstOrDefault(s => s.TemporaryId == model.CurrentEditingStudentTemporaryId.Value);
                if (studentToEdit != null)
                {
                    studentToEdit.CurrentStep = 3; 
                    PopulateViewBagForEducationalInformation(studentToEdit); 
                    SaveBulkApplicationToSession(model);
                    _logger.LogInformation($"Navigating student {studentToEdit.StudentName} (TempID: {studentToEdit.TemporaryId}) back to Educational Info (Step 3).");
                    return RedirectToAction("Index"); 
                }
                else
                {
                    _logger.LogWarning("PreviousToDocuments: Student with TempID {TempID} not found while trying to navigate back.", model.CurrentEditingStudentTemporaryId.Value);
                    TempData["ErrorMessage"] = "Student application not found. Cannot navigate back to documents.";
                }
            }
            else
            {
                _logger.LogWarning("PreviousToDocuments: CurrentEditingStudentTemporaryId is null. Cannot determine which student to navigate back for.");
                TempData["ErrorMessage"] = "No active student selected. Cannot navigate back to documents.";
            }
            
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult FinishCurrentStudentEditing()
        {
            var model = GetBulkApplicationFromSession();
            if (model.CurrentEditingStudentTemporaryId.HasValue)
            {
                _logger.LogInformation($"Finishing editing for student with TempID: {model.CurrentEditingStudentTemporaryId.Value}.");
                model.CurrentEditingStudentTemporaryId = null; 
                SaveBulkApplicationToSession(model);
                TempData["SuccessMessage"] = "Student editing finished and returned to list.";
            }
            else
            {
                _logger.LogWarning("FinishCurrentStudentEditing called but no student was being edited.");
                TempData["InfoMessage"] = "No student was actively being edited.";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        
        public async Task<IActionResult> UploadTemporaryFile(IFormFile file, Guid studentTemporaryId)
        {
            if (file == null || file.Length == 0)
            {
                return Json(new { success = false, message = "No file selected or file is empty." });
            }

            if (studentTemporaryId == Guid.Empty)
            {
                return Json(new { success = false, message = "Student identifier is missing." });
            }


            try
            {
              
                var temporaryUploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "temp_uploads", studentTemporaryId.ToString());
                if (!Directory.Exists(temporaryUploadsPath))
                {
                    Directory.CreateDirectory(temporaryUploadsPath);
                }

                var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                var filePath = Path.Combine(temporaryUploadsPath, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                _logger.LogInformation($"Temporary file uploaded: {filePath} for student TempID: {studentTemporaryId}");

                return Json(new 
                { 
                    success = true, 
                    temporaryFilePath = filePath, 
                    uniqueFileName = uniqueFileName,
                    originalFileName = file.FileName,
                    contentType = file.ContentType,
                    fileSize = file.Length
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading temporary file for student TempID: {StudentTemporaryId}", studentTemporaryId);
                return Json(new { success = false, message = "An error occurred while uploading the file." });
            }
        }
    }
} 