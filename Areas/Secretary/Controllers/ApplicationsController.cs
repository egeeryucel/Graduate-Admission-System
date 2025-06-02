using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GraduationAdmissionSystem.Data;
using GraduationAdmissionSystem.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;
using System;
using GraduationAdmissionSystem.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Linq;

namespace GraduationAdmissionSystem.Areas.Secretary.Controllers
{
    [Area("Secretary")]
    [Authorize(Roles = "Secretary")]
    public class ApplicationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ApplicationsController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public ApplicationsController(
            ApplicationDbContext context,
            ILogger<ApplicationsController> logger,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
        }
        public async Task<IActionResult> Pending()
        {
            try
            {
         
                var pendingApplications = await _context.ProgramSelections
                    .Include(ps => ps.ProgramChoices)
                    .Include(ps => ps.User)
                    .Where(ps => ps.ApplicationStatus == "Submitted")
                    .OrderByDescending(ps => ps.CreatedAt)
                    .ToListAsync();

                return View(pendingApplications);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Pending applications view: {ex.Message}");
                TempData["Error"] = "An error occurred while retrieving pending applications.";
                return RedirectToAction("Index", "Home");
            }
        }
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                _logger.LogInformation($"Fetching details for application ID: {id}");
                var application = await _context.ProgramSelections
                    .Include(ps => ps.User)
                    .Include(ps => ps.ProgramChoices)
                        .ThenInclude(pc => pc.Department) 
                    .FirstOrDefaultAsync(ps => ps.Id == id);

                if (application == null)
                {
                    _logger.LogWarning($"Application not found for ID: {id}");
                    TempData["Error"] = "Application not found.";
                    return RedirectToAction(nameof(Pending));
                }
                var applicantDepartmentIds = application.ProgramChoices
                                                 .Where(pc => pc.DepartmentId.HasValue)
                                                 .Select(pc => pc.DepartmentId.Value)
                                                 .Distinct()
                                                 .ToList();

                var allDepartmentChairs = await _userManager.GetUsersInRoleAsync("DepartmentChair");
                var relevantDepartmentChairs = new List<ApplicationUser>();

                if (applicantDepartmentIds.Any())
                {
                    foreach (var chair in allDepartmentChairs)
                    {
                        var chairWithManagedDepartments = await _context.Users
                            .Include(u => u.ManagedDepartments)
                            .FirstOrDefaultAsync(u => u.Id == chair.Id);

                        if (chairWithManagedDepartments?.ManagedDepartments != null &&
                            chairWithManagedDepartments.ManagedDepartments.Any(managedDept => applicantDepartmentIds.Contains(managedDept.DepartmentId)))
                        {
                            relevantDepartmentChairs.Add(chairWithManagedDepartments);
                        }
                    }
                }
                else
                {
                    _logger.LogWarning($"Application ID {id} has no program choices with a department.");

                }
                var distinctRelevantChairs = relevantDepartmentChairs.DistinctBy(u => u.Id).ToList();

                var chairsForSelectList = distinctRelevantChairs
                    .OrderBy(u => u.FirstName)
                    .ThenBy(u => u.LastName)
                    .Select(u => new {
                        Id = u.Id,
                        FullName = $"{u.FirstName} {u.LastName}"
                    })
                    .ToList();
                ViewBag.DepartmentChairs = new SelectList(chairsForSelectList, "Id", "FullName", application.AssignedDepartmentChairId);
                var personalInfo = await _context.PersonalInformations.FirstOrDefaultAsync(pi => pi.ProgramSelectionId == id);
                var educationalInfo = await _context.EducationalInformations.FirstOrDefaultAsync(ei => ei.ProgramSelectionId == id);
                var documents = await _context.Documents.Where(d => d.ProgramSelectionId == id).ToListAsync();

                 if (personalInfo == null || educationalInfo == null)
                 {
                     _logger.LogWarning($"Missing required personal/educational information for application ID: {id}");
                 }

                ViewBag.PersonalInfo = personalInfo;
                ViewBag.EducationalInfo = educationalInfo;
                ViewBag.Documents = documents;

                return View(application);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Application Details: {ex.Message}");
                TempData["Error"] = "An error occurred while retrieving application details.";
                return RedirectToAction(nameof(Pending));
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendBackForReview(int id, string reviewComment)
        {
            try
            {
                var application = await _context.ProgramSelections
                    .Include(ps => ps.User)
                    .FirstOrDefaultAsync(ps => ps.Id == id);

                if (application == null)
                {
                    _logger.LogWarning($"Application not found for ID: {id}");
                    TempData["Error"] = "Application not found.";
                    return RedirectToAction(nameof(Pending));
                }

                if (application.ApplicationStatus != "Submitted")
                {
                    _logger.LogWarning($"Cannot send back application with status: {application.ApplicationStatus}");
                    TempData["Error"] = "Only submitted applications can be sent back for review.";
                    return RedirectToAction(nameof(Details), new { id });
                }
                application.ApplicationStatus = "Needs Review";
                application.ReviewComment = reviewComment;
                await _context.SaveChangesAsync();

                TempData["Success"] = "Application has been sent back to the candidate for review.";
                return RedirectToAction(nameof(Pending));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending application back for review: {ex.Message}");
                TempData["Error"] = "An error occurred while processing your request.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForwardToDepartment(int id, string departmentChairId)
        {
            if (string.IsNullOrEmpty(departmentChairId))
            {
                TempData["Error"] = "Please select a Department Chair to forward the application.";
                return RedirectToAction(nameof(Details), new { id });
            }

            try
            {
                var application = await _context.ProgramSelections
                    .Include(ps => ps.User)
                    .FirstOrDefaultAsync(ps => ps.Id == id);

                if (application == null)
                {
                    _logger.LogWarning($"Application not found for ID: {id}");
                    TempData["Error"] = "Application not found.";
                    return RedirectToAction(nameof(Pending));
                }

                if (application.ApplicationStatus != "Submitted")
                {
                    _logger.LogWarning($"Cannot forward application with status: {application.ApplicationStatus}");
                    TempData["Error"] = "Only submitted applications can be forwarded.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var departmentChair = await _userManager.FindByIdAsync(departmentChairId);
                if (departmentChair == null || !(await _userManager.IsInRoleAsync(departmentChair, "DepartmentChair")))
                {
                     _logger.LogWarning($"Selected user ({departmentChairId}) is not a valid Department Chair for application ID: {id}.");
                     TempData["Error"] = "Invalid Department Chair selected.";
                     return RedirectToAction(nameof(Details), new { id });
                }

                application.AssignedDepartmentChairId = departmentChairId;
                application.ApplicationStatus = "Under Department Review";
                application.ForwardedToDepartmentAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                TempData["Success"] = $"Application has been forwarded to {departmentChair.FirstName} {departmentChair.LastName} for review.";
                return RedirectToAction(nameof(Pending));
            }
            catch (Exception ex)
            {
                 _logger.LogError($"Error forwarding application to department chair {departmentChairId}: {ex.Message}");
                 TempData["Error"] = "An error occurred while processing your request.";
                 return RedirectToAction(nameof(Details), new { id });
            }
        }

        public async Task<IActionResult> SentToDepartment()
        {
            try
            {
                var sentApplications = await _context.ProgramSelections
                    .Include(ps => ps.User) 
                    .Include(ps => ps.ProgramChoices)
                        .ThenInclude(pc => pc.Department) 
                    .Include(ps => ps.AssignedDepartmentChair) 
                    .Where(ps => ps.ApplicationStatus == "Under Department Review")
                    .OrderByDescending(ps => ps.ForwardedToDepartmentAt) 
                    .ToListAsync();

                return View(sentApplications);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in SentToDepartment applications view: {ex.Message}");
                TempData["Error"] = "An error occurred while retrieving applications sent to departments.";
                return RedirectToAction("Index", "Home");
            }
        }
    }
} 