using GraduationAdmissionSystem.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Identity;
using GraduationAdmissionSystem.Models;

namespace GraduationAdmissionSystem.Areas.Candidate.Controllers
{
    [Area("Candidate")]
    [Authorize(Roles = "Candidate")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var now = DateTime.UtcNow.Date;
            var openPeriod = await _context.ApplicationPeriods
                                        .FirstOrDefaultAsync(p => p.IsOpen && 
                                                                p.StartDate.Date <= now && 
                                                                p.EndDate.Date >= now);
            ViewBag.IsApplicationPeriodOpen = openPeriod != null;
            ViewBag.ApplicationPeriodName = openPeriod?.Name;

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                var latestDraftApplication = await _context.ProgramSelections
                    .Where(ps => ps.UserId == currentUser.Id && ps.ApplicationStatus == "Draft")
                    .OrderByDescending(ps => ps.CreatedAt)
                    .FirstOrDefaultAsync();
                
                ViewBag.LatestDraftApplicationId = latestDraftApplication?.Id;
            }
            else
            {
                ViewBag.LatestDraftApplicationId = null;
            }

            return View();
        }
    }
}
