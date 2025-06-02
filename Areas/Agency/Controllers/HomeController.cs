using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using GraduationAdmissionSystem.Models;
using GraduationAdmissionSystem.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace GraduationAdmissionSystem.Areas.Agency.Controllers
{
    [Area("Agency")]
    [Authorize(Roles = "Agency")]
    public class HomeController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public HomeController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var agencyUserId = _userManager.GetUserId(User);
            
            var submittedApplications = await _context.ProgramSelections
                                            .Where(ps => ps.SubmittedByAgencyUserId == agencyUserId)
                                            .Include(ps => ps.User)
                                            .Include(ps => ps.ProgramChoices)
                                            .OrderByDescending(ps => ps.CreatedAt)
                                            .ToListAsync();
            
            return View(submittedApplications);
        }

        
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "Application ID is missing.";
                return RedirectToAction(nameof(Index));
            }

            var agencyUserId = _userManager.GetUserId(User);

            var applicationDetails = await _context.ProgramSelections
                .Include(ps => ps.User) 
                .Include(ps => ps.ProgramChoices) 
                    .ThenInclude(pc => pc.Department) 
                .Include(ps => ps.PersonalInformation) 
                .Include(ps => ps.EducationalInformation) 
                .Include(ps => ps.Documents) 
                .Include(ps => ps.ApplicationPeriod) 
                .FirstOrDefaultAsync(ps => ps.Id == id && ps.SubmittedByAgencyUserId == agencyUserId);

            if (applicationDetails == null)
            {
                TempData["ErrorMessage"] = "Application not found or you do not have permission to view it.";
                return RedirectToAction(nameof(Index));
            }

            return View(applicationDetails);
        }
    }
} 