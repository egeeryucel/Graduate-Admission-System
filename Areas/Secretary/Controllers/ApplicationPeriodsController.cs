using GraduationAdmissionSystem.Data;
using GraduationAdmissionSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace GraduationAdmissionSystem.Areas.Secretary.Controllers
{
    [Area("Secretary")]
    [Authorize(Roles = "Secretary")]
    public class ApplicationPeriodsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ApplicationPeriodsController> _logger;

        public ApplicationPeriodsController(ApplicationDbContext context, ILogger<ApplicationPeriodsController> logger)
        {
            _context = context;
            _logger = logger;
        }
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Fetching all application periods for Secretary.");
            var periods = await _context.ApplicationPeriods.OrderByDescending(p => p.Year).ThenBy(p => p.Semester).ToListAsync();
            return View(periods);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Semester,Year,StartDate,EndDate,IsOpen")] ApplicationPeriod applicationPeriod)
        {
            if (ModelState.IsValid)
            {
                if (applicationPeriod.StartDate >= applicationPeriod.EndDate)
                {
                    ModelState.AddModelError("EndDate", "End Date must be after Start Date.");
                    return View(applicationPeriod);
                }

                var existingPeriod = await _context.ApplicationPeriods
                    .FirstOrDefaultAsync(p => p.Year == applicationPeriod.Year && p.Semester == applicationPeriod.Semester);
                
                if (existingPeriod != null)
                {
                    ModelState.AddModelError("", $"An application period for {applicationPeriod.Semester} {applicationPeriod.Year} already exists.");
                    return View(applicationPeriod);
                }
                if (string.IsNullOrWhiteSpace(applicationPeriod.Name))
                {
                    applicationPeriod.Name = $"{applicationPeriod.Semester} {applicationPeriod.Year}";
                }
                if (applicationPeriod.IsOpen)
                {
                    var anyOtherOpenPeriod = await _context.ApplicationPeriods.AnyAsync(p => p.IsOpen);
                    if (anyOtherOpenPeriod)
                    {
                        ModelState.AddModelError("IsOpen", "Another application period is already open. Only one period can be open at a time.");
                        return View(applicationPeriod);
                    }
                }

                _context.Add(applicationPeriod);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Created new application period: {applicationPeriod.Name}");
                TempData["Success"] = "Application period created successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(applicationPeriod);
        }
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var applicationPeriod = await _context.ApplicationPeriods.FindAsync(id);
            if (applicationPeriod == null)
            {
                return NotFound();
            }
            return View(applicationPeriod);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Semester,Year,StartDate,EndDate,IsOpen")] ApplicationPeriod applicationPeriod)
        {
            if (id != applicationPeriod.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                if (applicationPeriod.StartDate >= applicationPeriod.EndDate)
                {
                    ModelState.AddModelError("EndDate", "End Date must be after Start Date.");
                    return View(applicationPeriod);
                }
                var existingSameSemesterPeriod = await _context.ApplicationPeriods
                    .FirstOrDefaultAsync(p => p.Year == applicationPeriod.Year && p.Semester == applicationPeriod.Semester && p.Id != applicationPeriod.Id);

                if (existingSameSemesterPeriod != null)
                {
                     ModelState.AddModelError("", $"Another application period for {applicationPeriod.Semester} {applicationPeriod.Year} already exists.");
                    return View(applicationPeriod);
                }
                if (string.IsNullOrWhiteSpace(applicationPeriod.Name))
                {
                    applicationPeriod.Name = $"{applicationPeriod.Semester} {applicationPeriod.Year}";
                }
                if (applicationPeriod.IsOpen)
                {
                    var anyOtherOpenPeriod = await _context.ApplicationPeriods.AnyAsync(p => p.IsOpen && p.Id != applicationPeriod.Id);
                    if (anyOtherOpenPeriod)
                    {
                        ModelState.AddModelError("IsOpen", "Another application period is already open. Only one period can be open at a time.");
                        return View(applicationPeriod);
                    }
                }

                try
                {
                    _context.Update(applicationPeriod);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Updated application period: {applicationPeriod.Name}");
                    TempData["Success"] = "Application period updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ApplicationPeriodExists(applicationPeriod.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        _logger.LogError($"Concurrency error while updating period ID {applicationPeriod.Id}.");
                        TempData["Error"] = "Could not update the period due to a concurrency issue. Please try again.";
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(applicationPeriod);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var applicationPeriod = await _context.ApplicationPeriods.FindAsync(id);
            if (applicationPeriod == null)
            {
                TempData["Error"] = "Application period not found.";
                return RedirectToAction(nameof(Index));
            }
            bool willBeOpen = !applicationPeriod.IsOpen; 

            if (willBeOpen) 
            {
                var anyOtherOpenPeriod = await _context.ApplicationPeriods.AnyAsync(p => p.IsOpen && p.Id != applicationPeriod.Id);
                if (anyOtherOpenPeriod)
                {
                    TempData["Error"] = "Cannot open this period. Another application period is already open. Only one period can be open at a time.";
                    return RedirectToAction(nameof(Index));
                }
            }

            applicationPeriod.IsOpen = !applicationPeriod.IsOpen;
            _context.Update(applicationPeriod);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Application period '{applicationPeriod.Name}' has been {(applicationPeriod.IsOpen ? "opened" : "closed")}.";
            _logger.LogInformation($"Toggled status for application period {applicationPeriod.Name} to {(applicationPeriod.IsOpen ? "Open" : "Closed")}.");
            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var applicationPeriod = await _context.ApplicationPeriods
                .FirstOrDefaultAsync(m => m.Id == id);
            if (applicationPeriod == null)
            {
                return NotFound();
            }
            var hasLinkedProgramSelections = await _context.ProgramSelections.AnyAsync(ps => ps.ApplicationPeriodId == id);
            if (hasLinkedProgramSelections)
            {
                ViewData["ErrorMessage"] = $"This application period ('{applicationPeriod.Name}') cannot be deleted because it has program selections linked to it. Please remove or reassign these program selections first.";
            }

            return View(applicationPeriod);
        }
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var applicationPeriod = await _context.ApplicationPeriods.FindAsync(id);
            if (applicationPeriod == null)
            {
                TempData["Error"] = "Application period not found.";
                return RedirectToAction(nameof(Index));
            }
            var hasLinkedProgramSelections = await _context.ProgramSelections.AnyAsync(ps => ps.ApplicationPeriodId == id);
            if (hasLinkedProgramSelections)
            {
                TempData["Error"] = $"Error: Application period '{applicationPeriod.Name}' could not be deleted because it still has program selections linked to it.";
                return RedirectToAction(nameof(Index)); 
            }

            try
            {
                _context.ApplicationPeriods.Remove(applicationPeriod);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Application period '{applicationPeriod.Name}' has been deleted successfully.";
                _logger.LogInformation($"Deleted application period: {applicationPeriod.Name}");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, $"Error deleting application period {applicationPeriod.Name}.");
                TempData["Error"] = "An error occurred while deleting the application period. It might be in use.";
            }
            
            return RedirectToAction(nameof(Index));
        }

        private bool ApplicationPeriodExists(int id)
        {
            return _context.ApplicationPeriods.Any(e => e.Id == id);
        }
    }
} 