using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using GraduationAdmissionSystem.Models;
using GraduationAdmissionSystem.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace GraduationAdmissionSystem.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public IActionResult Index()
    {
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            if (User.IsInRole("Admin"))
            {
                return RedirectToAction("Index", "Home", new { area = "Admin" });
            }
            else if (User.IsInRole("Secretary"))
            {
                return RedirectToAction("Index", "Home", new { area = "Secretary" });
            }
            else if (User.IsInRole("DepartmentChair"))
            {
                return RedirectToAction("Index", "Home", new { area = "DepartmentChair" });
            }
            else if (User.IsInRole("Agency"))
            {
                return RedirectToAction("Index", "Home", new { area = "Agency" });
            }
            else if (User.IsInRole("Candidate"))
            {
                return RedirectToAction("Index", "Home", new { area = "Candidate" });
            }
        }
        
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [Authorize]
    [HttpGet]
    public IActionResult CheckStatus()
    {
        return View();
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CheckStatus(string trackingNumber)
    {
        if (string.IsNullOrEmpty(trackingNumber))
        {
            TempData["Error"] = "Please enter a tracking number.";
            return View();
        }

        var application = await _context.ProgramSelections
            .Include(ps => ps.ProgramChoices)
            .FirstOrDefaultAsync(ps => ps.TrackingNumber == trackingNumber);

        if (application == null)
        {
            TempData["Error"] = "Application not found. Please check your tracking number.";
            return View();
        }
        var personalInfo = await _context.PersonalInformations
            .FirstOrDefaultAsync(pi => pi.ProgramSelectionId == application.Id);
            
        var educationalInfo = await _context.EducationalInformations
            .FirstOrDefaultAsync(ei => ei.ProgramSelectionId == application.Id);
            
        var documents = await _context.Documents
            .Where(d => d.ProgramSelectionId == application.Id)
            .ToListAsync();

        ViewBag.PersonalInfo = personalInfo;
        ViewBag.EducationalInfo = educationalInfo;
        ViewBag.Documents = documents;

        return View("ApplicationStatus", application);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
