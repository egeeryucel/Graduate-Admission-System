using GraduationAdmissionSystem.Data;
using GraduationAdmissionSystem.Models;
using GraduationAdmissionSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace GraduationAdmissionSystem.Areas.DepartmentChair.Controllers
{
    [Area("DepartmentChair")]
    [Authorize(Roles = "DepartmentChair")] 
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<HomeController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

       
        public IActionResult Index()
        {
        
            return View();
        }

       
        public async Task<IActionResult> Applications()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                _logger.LogWarning("User not found while trying to fetch applications for Department Chair.");
                TempData["Error"] = "User not found. Please log in again.";
                return Challenge(); 
            }

            _logger.LogInformation($"Fetching applications for Department Chair: {currentUser.UserName} (ID: {currentUser.Id}).");

            var applicationsForReview = await _context.ProgramSelections
                .Include(ps => ps.User) 
                .Include(ps => ps.ProgramChoices)
                    .ThenInclude(pc => pc.Department) 
                .Include(ps => ps.ApplicationPeriod) 
                .Where(ps => ps.AssignedDepartmentChairId == currentUser.Id && 
                               ps.ApplicationStatus == "Under Department Review")
                .OrderByDescending(ps => ps.ForwardedToDepartmentAt) 
                .ToListAsync();

            _logger.LogInformation($"Found {applicationsForReview.Count} applications for review for Department Chair: {currentUser.UserName}.");

            return View(applicationsForReview);
        }

        
        public async Task<IActionResult> Details(int? id)
        {
             if (id == null)
            {
                _logger.LogWarning("Details action called with null ID.");
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                 _logger.LogWarning($"User not found while trying to view details for application ID: {id}");
                 TempData["Error"] = "User not found. Please log in again.";
                 return Challenge();
            }

            _logger.LogInformation($"Department Chair {currentUser.UserName} fetching details for application ID: {id}");

            try
            {
                var application = await _context.ProgramSelections
                    .Include(ps => ps.User)
                    .Include(ps => ps.ProgramChoices.OrderBy(pc => pc.Choice)) 
                        .ThenInclude(pc => pc.Department)
                    .Include(ps => ps.ApplicationPeriod) 
                    .FirstOrDefaultAsync(ps => ps.Id == id);

                if (application == null)
                {
                    _logger.LogWarning($"Application not found for ID: {id}");
                    TempData["Error"] = "Application not found.";
                    return RedirectToAction(nameof(Applications));
                }

               
                if (application.AssignedDepartmentChairId != currentUser.Id ||
                    !(application.ApplicationStatus == "Under Department Review" ||
                      application.ApplicationStatus == "Interview Passed" ||
                      application.ApplicationStatus == "Accepted" ||
                      application.ApplicationStatus == "Rejected"))
                {
                     _logger.LogWarning($"Access denied for Department Chair {currentUser.UserName} to application ID: {id}. AssignedChair: {application.AssignedDepartmentChairId}, Status: {application.ApplicationStatus}");
                     TempData["Error"] = "You do not have permission to view this application or it is not in the correct state for review.";
                     return RedirectToAction(nameof(Applications));
                }

                var viewModel = new DepartmentChairApplicationDetailsViewModel
                {
                    ApplicationId = application.Id,
                    ApplicantFullName = $"{application.User?.FirstName} {application.User?.LastName}",
                    ApplicantEmail = application.User?.Email,
                    ApplicationStatus = application.ApplicationStatus,
                    ApplicationCreatedAt = application.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                    ApplicationForwardedAt = application.ForwardedToDepartmentAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "N/A",
                    ApplicationPeriodName = application.ApplicationPeriod?.Name ?? "N/A",
                    ProgramChoices = application.ProgramChoices,
                    FinalDecisionNotes = application.FinalDecisionNotes,
                    DepartmentChairNotes = application.DepartmentChairNotes,
                    RequiresScientificPreparation = application.RequiresScientificPreparation
                };

                _logger.LogInformation($"Application ID: {application.Id} - Period ID from application: {application.ApplicationPeriodId}, Period Name: {application.ApplicationPeriod?.Name}");
                int? targetProgramId = DetermineTargetProgramId(application);
                _logger.LogInformation($"Application ID: {application.Id} - Determined Target Program ID: {targetProgramId}");

                if (targetProgramId.HasValue && application.ApplicationPeriodId.HasValue)
                {
                    _logger.LogInformation($"Application ID: {application.Id} - Attempting to load scholarship quotas with TargetProgramId: {targetProgramId.Value} and ApplicationPeriodId: {application.ApplicationPeriodId.Value}");
                    await LoadScholarshipQuotasForViewModel(viewModel, targetProgramId.Value, application.ApplicationPeriodId.Value);
                }
                else
                {
                    _logger.LogWarning($"Application ID: {application.Id} - Cannot load scholarship quotas. TargetProgramId: {targetProgramId}, ApplicationPeriodId: {application.ApplicationPeriodId}. Scholarship options cannot be loaded.");
                    if (!targetProgramId.HasValue) TempData["Warning"] = "Could not determine the target program for this application. Scholarship options cannot be loaded.";
                    if (!application.ApplicationPeriodId.HasValue) TempData["Warning"] += " Application period is missing. Scholarship options cannot be loaded.";
                    viewModel.AvailableScholarshipQuotas = new List<SelectListItem> { new SelectListItem("Unable to load scholarships", "") };
                }

            
                ViewBag.PersonalInfo = await _context.PersonalInformations.FirstOrDefaultAsync(pi => pi.ProgramSelectionId == id);
                ViewBag.EducationalInfo = await _context.EducationalInformations.FirstOrDefaultAsync(ei => ei.ProgramSelectionId == id);
                ViewBag.Documents = await _context.Documents.Where(d => d.ProgramSelectionId == id).ToListAsync();

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving details for application ID {id} for Department Chair {currentUser.UserName}: {ex.Message}", ex);
                TempData["Error"] = "An error occurred while retrieving application details.";
                return RedirectToAction(nameof(Applications));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptApplication(DepartmentChairApplicationDetailsViewModel viewModel)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                TempData["Error"] = "User session expired. Please log in again.";
                return Challenge();
            }

            var application = await _context.ProgramSelections
                                .Include(ps => ps.User)
                                .Include(ps => ps.ProgramChoices)
                                .Include(ps => ps.ApplicationPeriod)
                                .FirstOrDefaultAsync(ps => ps.Id == viewModel.ApplicationId);

            if (application == null)
            {
                _logger.LogWarning($"AcceptApplication POST: Application not found for ID: {viewModel.ApplicationId}.");
                TempData["Error"] = "Application not found.";
                return RedirectToAction(nameof(Applications));
            }
            
            if (application.AssignedDepartmentChairId != currentUser.Id || 
                (application.ApplicationStatus != "Under Department Review" && application.ApplicationStatus != "Interview Passed"))
            {
                _logger.LogWarning($"Access denied or invalid state for Department Chair {currentUser.UserName} to accept application ID: {application.Id}. AssignedChair: {application.AssignedDepartmentChairId}, Status: {application.ApplicationStatus}");
                TempData["Error"] = "You do not have permission to accept this application or it is not in the correct state for review.";
                await RepopulateViewModelForError(viewModel, application);
                return View("Details", viewModel);
            }
            
            int? targetProgramId = DetermineTargetProgramId(application);

           
            if (!ModelState.IsValid)
            {
                _logger.LogWarning($"ModelState invalid for AcceptApplication POST (before main check). ApplicationId: {viewModel.ApplicationId}. Errors: {string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))}");
            }

            if (ModelState.IsValid)
            {
                var selectedScholarshipQuota = await _context.ScholarshipQuotas.FindAsync(viewModel.SelectedScholarshipQuotaId);
                if (selectedScholarshipQuota == null)
                {
                    _logger.LogWarning($"AcceptApplication ModelState - selectedScholarshipQuota is null for Id: {viewModel.SelectedScholarshipQuotaId}");
                    ModelState.AddModelError(nameof(viewModel.SelectedScholarshipQuotaId), "Invalid scholarship option selected.");
                }
                else
                {
                    var programQuotaForSelectedSQ = await _context.ProgramQuotas.FindAsync(selectedScholarshipQuota.ProgramQuotaId);
                    if (programQuotaForSelectedSQ == null || !targetProgramId.HasValue ||
                        programQuotaForSelectedSQ.ProgramId != targetProgramId.Value ||
                        programQuotaForSelectedSQ.ApplicationPeriodId != application.ApplicationPeriodId)
                    {
                        _logger.LogWarning($"AcceptApplication ModelState - Scholarship does not belong to program/period. SQProgramId: {programQuotaForSelectedSQ?.ProgramId}, TargetProgramId: {targetProgramId}, SQAppPeriodId: {programQuotaForSelectedSQ?.ApplicationPeriodId}, AppPeriodId: {application.ApplicationPeriodId}");
                        ModelState.AddModelError(nameof(viewModel.SelectedScholarshipQuotaId), "The selected scholarship does not belong to the program/period.");
                    }
                    else
                    {
                        int awardedCount = await _context.ProgramSelections
                                                   .CountAsync(ps => ps.AwardedScholarshipQuotaId == viewModel.SelectedScholarshipQuotaId &&
                                                                      ps.ApplicationStatus == "Accepted" && ps.Id != application.Id);
                        if (awardedCount >= selectedScholarshipQuota.AllocatedQuota)
                        {
                            _logger.LogWarning($"AcceptApplication ModelState - No remaining slots for scholarship. Awarded: {awardedCount}, Quota: {selectedScholarshipQuota.AllocatedQuota}");
                            ModelState.AddModelError(nameof(viewModel.SelectedScholarshipQuotaId), "No remaining slots for this scholarship.");
                        }
                    }
                }

                if (ModelState.IsValid)
                {
                    try
                    {
                        application.ApplicationStatus = "Accepted";
                        application.ScholarshipRate = selectedScholarshipQuota.ScholarshipPercentage;
                        application.AwardedScholarshipQuotaId = selectedScholarshipQuota.ScholarshipQuotaId;
                        application.FinalDecisionNotes = viewModel.FinalDecisionNotes;
                        application.RequiresScientificPreparation = viewModel.RequiresScientificPreparation;
                        application.DepartmentDecisionDate = DateTime.UtcNow;
                        application.RejectionReason = null;

                        _context.ProgramSelections.Update(application);
                        await _context.SaveChangesAsync();

                        _logger.LogInformation($"Application ID: {application.Id} successfully accepted by Department Chair {currentUser.UserName} with ScholarshipQuotaId: {selectedScholarshipQuota.ScholarshipQuotaId} ({selectedScholarshipQuota.ScholarshipPercentage}%).");
                        TempData["Success"] = $"Application successfully accepted with {selectedScholarshipQuota.ScholarshipPercentage}% scholarship.";
                        return RedirectToAction(nameof(Applications));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error accepting application ID {application.Id} by Department Chair {currentUser.UserName}: {ex.Message}", ex);
                        ModelState.AddModelError("", "An error occurred while accepting the application. " + ex.Message);
                    }
                }
            }
            _logger.LogWarning($"ModelState invalid for AcceptApplication POST. ApplicationId: {viewModel.ApplicationId}. Errors: {string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))}");
            await RepopulateViewModelForError(viewModel, application);
            return View("Details", viewModel);
        }
        
        private int? DetermineTargetProgramId(ProgramSelection application)
        {
            _logger.LogInformation($"DetermineTargetProgramId called for Application ID: {application.Id}. Choices count: {application.ProgramChoices.Count}, Lang: {application.Language}, Level: {application.ProgramLevel}.");
            if (application.ProgramChoices.Any() &&
                application.ProgramChoices.First().DepartmentId.HasValue &&
                !string.IsNullOrEmpty(application.Language) &&
                !string.IsNullOrEmpty(application.ProgramLevel))
            {
                var firstChoice = application.ProgramChoices.OrderBy(pc => pc.Choice).First();
                var firstChoiceDepartmentId = firstChoice.DepartmentId.Value;
                var applicationLanguage = application.Language;
                var applicationLevel = application.ProgramLevel;

                _logger.LogInformation($"Attempting to find program with DepartmentId: {firstChoiceDepartmentId}, Language: {applicationLanguage}, Level: {applicationLevel} for Application ID: {application.Id}. First choice program name hint: {firstChoice.ProgramName}");

                var programForChoice = _context.Programs
                                            .FirstOrDefault(p => p.DepartmentId == firstChoiceDepartmentId &&
                                                                 p.Language == applicationLanguage &&
                                                                 p.Level == applicationLevel);

                if (programForChoice != null)
                {
                    _logger.LogInformation($"Found Program for AppId: {application.Id}. ProgramId: {programForChoice.ProgramId}, Name: {programForChoice.Name}, NameEn: {programForChoice.NameEn}, Level: {programForChoice.Level}, Lang: {programForChoice.Language}, DeptId: {programForChoice.DepartmentId}");
                    return programForChoice.ProgramId;
                }
                _logger.LogWarning($"Could not find Program for AppId: {application.Id} with DeptId: {firstChoiceDepartmentId}, Lang: {applicationLanguage}, Level: {applicationLevel}. First choice program name from application was '{firstChoice.ProgramName}'. Searched Programs table with these criteria.");
            }
            else
            {
                _logger.LogWarning($"Could not determine TargetProgramId for AppId: {application.Id}. Missing critical info: ProgramChoice.Any()={application.ProgramChoices.Any()}, FirstChoice.DeptId.HasValue={application.ProgramChoices.FirstOrDefault()?.DepartmentId.HasValue}, Lang='{application.Language}', Level='{application.ProgramLevel}'.");
            }
            return null;
        }

        private async Task LoadScholarshipQuotasForViewModel(DepartmentChairApplicationDetailsViewModel viewModel, int targetProgramId, int applicationPeriodId)
        {
            _logger.LogInformation($"LoadScholarshipQuotasForViewModel called for TargetProgramId: {targetProgramId}, ApplicationPeriodId: {applicationPeriodId}.");
            var programQuota = await _context.ProgramQuotas
                .Include(pq => pq.ScholarshipQuotas)
                .FirstOrDefaultAsync(pq => pq.ProgramId == targetProgramId && pq.ApplicationPeriodId == applicationPeriodId);

            if (programQuota != null)
            {
                _logger.LogInformation($"Found ProgramQuota for TargetProgramId: {targetProgramId}, ApplicationPeriodId: {applicationPeriodId}. ProgramQuotaId: {programQuota.ProgramQuotaId}, Number of ScholarshipQuotas: {programQuota.ScholarshipQuotas.Count}.");
                var availableScholarships = new List<SelectListItem> { new SelectListItem("Select Scholarship...", "") };
                foreach (var sq in programQuota.ScholarshipQuotas.OrderBy(s => s.ScholarshipPercentage))
                {
                    int awardedCount = await _context.ProgramSelections.CountAsync(ps => ps.AwardedScholarshipQuotaId == sq.ScholarshipQuotaId && ps.ApplicationStatus == "Accepted");
                    int remainingSlots = sq.AllocatedQuota - awardedCount;
                    _logger.LogInformation($"ScholarshipQuotaId: {sq.ScholarshipQuotaId}, Percentage: {sq.ScholarshipPercentage}%, Allocated: {sq.AllocatedQuota}, Awarded: {awardedCount}, Remaining: {remainingSlots}");
                    if (remainingSlots > 0)
                    {
                        availableScholarships.Add(new SelectListItem
                        {
                            Value = sq.ScholarshipQuotaId.ToString(),
                            Text = $"{sq.ScholarshipPercentage}% Scholarship ({remainingSlots} slot(s) remaining)"
                        });
                    }
                }
                viewModel.AvailableScholarshipQuotas = availableScholarships;
                if (availableScholarships.Count <= 1)
                {
                    _logger.LogWarning($"No scholarship options with available slots found for ProgramQuotaId: {programQuota.ProgramQuotaId}.");
                    TempData["Warning"] = "No scholarship options with available slots found.";
                }
            }
            else
            {
                _logger.LogWarning($"ProgramQuota info not found for TargetProgramId: {targetProgramId} and ApplicationPeriodId: {applicationPeriodId}. This is why 'Program quota info not found' is displayed.");
                TempData["Error"] = "Program quota info not found."; 
                viewModel.AvailableScholarshipQuotas = new List<SelectListItem> { new SelectListItem("Quota not found", "") };
            }
        }

        private async Task RepopulateViewModelForError(DepartmentChairApplicationDetailsViewModel viewModel, ProgramSelection application)
        {
            viewModel.ApplicationId = application.Id;
            viewModel.ApplicantFullName = $"{application.User?.FirstName} {application.User?.LastName}";
            viewModel.ApplicantEmail = application.User?.Email;
            viewModel.ApplicationStatus = application.ApplicationStatus;
            viewModel.ApplicationCreatedAt = application.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
            viewModel.ApplicationForwardedAt = application.ForwardedToDepartmentAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "N/A";
            viewModel.ApplicationPeriodName = application.ApplicationPeriod?.Name ?? "N/A";
            viewModel.ProgramChoices = application.ProgramChoices;
            viewModel.DepartmentChairNotes = application.DepartmentChairNotes;

            int? targetProgramId = DetermineTargetProgramId(application);
            if (targetProgramId.HasValue && application.ApplicationPeriodId.HasValue)
            {
                await LoadScholarshipQuotasForViewModel(viewModel, targetProgramId.Value, application.ApplicationPeriodId.Value);
                var selectedExists = viewModel.AvailableScholarshipQuotas.Any(sq => sq.Value == viewModel.SelectedScholarshipQuotaId.ToString());
                if(selectedExists)
                {
                    foreach(var item in viewModel.AvailableScholarshipQuotas)
                    {
                        item.Selected = item.Value == viewModel.SelectedScholarshipQuotaId.ToString();
                    }
                }
                 else if (viewModel.SelectedScholarshipQuotaId > 0)
                {
                   
                }
            }
            else { viewModel.AvailableScholarshipQuotas = new List<SelectListItem> { new SelectListItem("Unable to load scholarships", "") }; }

            ViewBag.PersonalInfo = await _context.PersonalInformations.FirstOrDefaultAsync(pi => pi.ProgramSelectionId == application.Id);
            ViewBag.EducationalInfo = await _context.EducationalInformations.FirstOrDefaultAsync(ei => ei.ProgramSelectionId == application.Id);
            ViewBag.Documents = await _context.Documents.Where(d => d.ProgramSelectionId == application.Id).ToListAsync();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecommendForInterview(int id, string interviewNotes)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) { TempData["Error"] = "User not found."; return Challenge(); }

            _logger.LogInformation($"Department Chair {currentUser.UserName} attempting to recommend application ID: {id} for interview.");
            var application = await _context.ProgramSelections.FirstOrDefaultAsync(ps => ps.Id == id);

            if (application == null) { _logger.LogWarning($"RecommendForInterview: Application not found for ID: {id}."); TempData["Error"] = "Application not found."; return RedirectToAction(nameof(Applications)); }

            if (application.AssignedDepartmentChairId != currentUser.Id || application.ApplicationStatus != "Under Department Review")
            {
                _logger.LogWarning($"Access denied or invalid state for RecommendForInterview on application ID: {id} by {currentUser.UserName}.");
                TempData["Error"] = "Cannot recommend this application for interview at its current state or insufficient permissions.";
                return RedirectToAction(nameof(Details), new { id = id });
            }

            try
            {
                application.ApplicationStatus = "Pending Interview"; 
                application.DepartmentChairNotes = $"Recommended for interview. Notes: {interviewNotes}"; 
                application.DepartmentDecisionDate = DateTime.UtcNow; 

                _context.ProgramSelections.Update(application);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Application ID: {id} successfully recommended for interview by {currentUser.UserName}.");
                TempData["Success"] = "Application successfully recommended for interview.";
                return RedirectToAction(nameof(Applications));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error recommending application ID {id} for interview by {currentUser.UserName}: {ex.Message}", ex);
                TempData["Error"] = "An error occurred while recommending for interview.";
                return RedirectToAction(nameof(Details), new { id = id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectApplication(int id, string rejectionReason)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) { TempData["Error"] = "User not found."; return Challenge(); }

             _logger.LogInformation($"Department Chair {currentUser.UserName} attempting to reject application ID: {id}.");
            var application = await _context.ProgramSelections.FirstOrDefaultAsync(ps => ps.Id == id);

            if (application == null) { _logger.LogWarning($"RejectApplication: Application not found for ID: {id}."); TempData["Error"] = "Application not found."; return RedirectToAction(nameof(Applications)); }

            if (string.IsNullOrWhiteSpace(rejectionReason))
            {
                TempData["Error"] = "Rejection reason cannot be empty.";
                return RedirectToAction(nameof(Details), new { id = id }); 
            }

            if (application.AssignedDepartmentChairId != currentUser.Id || 
                (application.ApplicationStatus != "Under Department Review" && application.ApplicationStatus != "Interview Passed"))
            {
                 _logger.LogWarning($"Access denied or invalid state for RejectApplication on application ID: {id} by {currentUser.UserName}.");
                TempData["Error"] = "Cannot reject this application at its current state or insufficient permissions.";
                return RedirectToAction(nameof(Details), new { id = id });
            }

            try
            {
                application.ApplicationStatus = "Rejected";
                application.RejectionReason = rejectionReason;
                application.DepartmentDecisionDate = DateTime.UtcNow;
                application.FinalDecisionNotes = null; 
                application.ScholarshipRate = null; 
                application.AwardedScholarshipQuotaId = null; 

                _context.ProgramSelections.Update(application);
                await _context.SaveChangesAsync();
                 _logger.LogInformation($"Application ID: {id} successfully rejected by {currentUser.UserName}. Reason: {rejectionReason}");
                TempData["Success"] = "Application successfully rejected.";
                return RedirectToAction(nameof(Applications));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error rejecting application ID {id} by {currentUser.UserName}: {ex.Message}", ex);
                TempData["Error"] = "An error occurred while rejecting the application.";
                return RedirectToAction(nameof(Details), new { id = id });
            }
        }

        public async Task<IActionResult> DecisionHistory(string searchString, string status, int? applicationPeriodId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                TempData["Error"] = "User not found. Please log in again.";
                return Challenge();
            }

            _logger.LogInformation($"Department Chair {currentUser.UserName} fetching decision history. Filters: Search='{searchString}', Status='{status}', PeriodId='{applicationPeriodId}'.");

            try
            {
                var query = _context.ProgramSelections
                    .Include(ps => ps.User) 
                    .Include(ps => ps.ProgramChoices) 
                        .ThenInclude(pc => pc.Department) 
                    .Include(ps => ps.ApplicationPeriod) 
                    .Where(ps => ps.AssignedDepartmentChairId == currentUser.Id &&
                                 (ps.ApplicationStatus == "Accepted" || ps.ApplicationStatus == "Rejected"));

                if (!string.IsNullOrEmpty(searchString))
                {
                    query = query.Where(ps => 
                        (ps.User.FirstName != null && ps.User.FirstName.Contains(searchString)) || 
                        (ps.User.LastName != null && ps.User.LastName.Contains(searchString)) || 
                        (ps.User.Email != null && ps.User.Email.Contains(searchString)) ||
                        (ps.TrackingNumber != null && ps.TrackingNumber.Contains(searchString)) ||
                        (ps.ProgramChoices.Any(pc => pc.ProgramName != null && pc.ProgramName.Contains(searchString)))
                    );
                }

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(ps => ps.ApplicationStatus == status);
                }

                if (applicationPeriodId.HasValue)
                {
                    query = query.Where(ps => ps.ApplicationPeriodId == applicationPeriodId.Value);
                }

                var decidedApplications = await query
                    .OrderByDescending(ps => ps.DepartmentDecisionDate) 
                    .ToListAsync();
                
                _logger.LogInformation($"Found {decidedApplications.Count} decided applications for Department Chair {currentUser.UserName} with applied filters.");

                ViewBag.CurrentFilter = searchString;
                ViewBag.CurrentStatus = status;
                ViewBag.CurrentPeriodId = applicationPeriodId;
                ViewBag.ApplicationPeriods = new SelectList(await _context.ApplicationPeriods.Where(ap => ap.IsOpen).OrderBy(ap => ap.Name).ToListAsync(), "Id", "Name", applicationPeriodId);
                ViewBag.StatusOptions = new SelectList(new List<SelectListItem>
                {
                    new SelectListItem { Value = "", Text = "All Statuses" },
                    new SelectListItem { Value = "Accepted", Text = "Accepted" },
                    new SelectListItem { Value = "Rejected", Text = "Rejected" }
                }, "Value", "Text", status);


                return View(decidedApplications);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching decision history for Department Chair {currentUser.UserName}: {ex.Message}", ex);
                TempData["Error"] = "An error occurred while fetching the decision history.";
                return RedirectToAction("Index"); 
            }
        }

        public async Task<IActionResult> ManageInterviews()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                TempData["Error"] = "User not found. Please log in again.";
                return Challenge();
            }

            _logger.LogInformation($"Department Chair {currentUser.UserName} fetching applications for interview management.");

            try
            {
                var applicationsForInterview = await _context.ProgramSelections
                    .Include(ps => ps.User)
                    .Include(ps => ps.ProgramChoices)
                        .ThenInclude(pc => pc.Department)
                    .Include(ps => ps.ApplicationPeriod)
                    .Where(ps => ps.AssignedDepartmentChairId == currentUser.Id &&
                                 ps.ApplicationStatus == "Pending Interview") 
                    .OrderByDescending(ps => ps.DepartmentDecisionDate) 
                    .ToListAsync();
                
                _logger.LogInformation($"Found {applicationsForInterview.Count} applications pending interview for Department Chair {currentUser.UserName}.");
                return View(applicationsForInterview);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching applications for interview management for {currentUser.UserName}: {ex.Message}", ex);
                TempData["Error"] = "An error occurred while fetching applications for interview management.";
                return RedirectToAction("Index"); 
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetInterviewResult(int applicationId, string interviewResult, string interviewNotes)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) 
            { 
                TempData["Error"] = "User not found. Please log in again."; 
                return Challenge(); 
            }

            _logger.LogInformation($"Department Chair {currentUser.UserName} setting interview result for ApplicationId: {applicationId}. Result: {interviewResult}");

            var application = await _context.ProgramSelections.FirstOrDefaultAsync(ps => ps.Id == applicationId);

            if (application == null)
            {
                _logger.LogWarning($"SetInterviewResult: Application not found for ID: {applicationId}.");
                TempData["Error"] = "Application not found.";
                return RedirectToAction(nameof(ManageInterviews));
            }

            if (application.AssignedDepartmentChairId != currentUser.Id || application.ApplicationStatus != "Pending Interview")
            {
                _logger.LogWarning($"Access denied or invalid state for SetInterviewResult on application ID: {applicationId} by {currentUser.UserName}. Current Status: {application.ApplicationStatus}");
                TempData["Error"] = "Cannot set interview result for this application at its current state or insufficient permissions.";
                return RedirectToAction(nameof(ManageInterviews));
            }

            if (string.IsNullOrEmpty(interviewResult) || (interviewResult != "Passed" && interviewResult != "Failed"))
            {
                TempData["Error"] = "Invalid interview result provided.";
                return RedirectToAction(nameof(ManageInterviews)); 
            }

            try
            {
                if (interviewResult == "Passed")
                {
                    application.ApplicationStatus = "Interview Passed";
                    TempData["Success"] = $"Interview for application {application.TrackingNumber} marked as Passed. Please review and finalize the decision.";
                    application.DepartmentChairNotes = $"Interview Result: Passed. Notes: {interviewNotes}";
                    application.DepartmentDecisionDate = DateTime.UtcNow;
                    _context.ProgramSelections.Update(application);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Interview result for Application ID: {applicationId} successfully set to {application.ApplicationStatus} by {currentUser.UserName}. Redirecting to Details.");
                    return RedirectToAction(nameof(Details), new { id = applicationId });
                }
                else 
                {
                    application.ApplicationStatus = "Interview Failed";
                    TempData["Success"] = $"Interview result for application tracking ID {application.TrackingNumber} has been set to '{application.ApplicationStatus}'.";
                    application.DepartmentChairNotes = $"Interview Result: Failed. Notes: {interviewNotes}";
                    application.DepartmentDecisionDate = DateTime.UtcNow;
                    _context.ProgramSelections.Update(application);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Interview result for Application ID: {applicationId} successfully set to {application.ApplicationStatus} by {currentUser.UserName}. Redirecting to ManageInterviews.");
                    return RedirectToAction(nameof(ManageInterviews));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error setting interview result for application ID {applicationId} by {currentUser.UserName}: {ex.Message}", ex);
                TempData["Error"] = "An error occurred while setting the interview result.";
                return RedirectToAction(nameof(ManageInterviews));
            }
        }
    }
} 