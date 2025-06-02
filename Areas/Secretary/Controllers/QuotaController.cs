using GraduationAdmissionSystem.Data;
using GraduationAdmissionSystem.Models;
using GraduationAdmissionSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;

namespace GraduationAdmissionSystem.Areas.Secretary.Controllers
{
    [Area("Secretary")]
    [Authorize(Roles = "Secretary")]
    public class QuotaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<QuotaController> _logger;

        public QuotaController(ApplicationDbContext context, ILogger<QuotaController> logger)
        {
            _context = context;
            _logger = logger;
        }
        public async Task<IActionResult> Index()
        {
            ViewBag.ApplicationPeriods = new SelectList(await _context.ApplicationPeriods.Where(p => p.IsOpen).ToListAsync(), "ApplicationPeriodId", "Name");
            var programQuotas = await _context.ProgramQuotas
                .Include(pq => pq.Program)
                .Include(pq => pq.ApplicationPeriod)
                .Include(pq => pq.ScholarshipQuotas)
                .OrderBy(pq => pq.ApplicationPeriod.Name).ThenBy(pq => pq.Program.Name)
                .ToListAsync();
            return View(programQuotas);
        }
        public async Task<IActionResult> Create()
        {
            var applicationPeriods = await _context.ApplicationPeriods
                                               .Where(ap => ap.IsOpen && !string.IsNullOrEmpty(ap.Name))
                                               .OrderBy(ap => ap.Name)
                                               .ToListAsync();

            var viewModel = new ProgramQuotaCreateViewModel
            {
                ApplicationPeriodSelectList = new SelectList(applicationPeriods, "Id", "Name"),
                LanguageSelectList = new List<SelectListItem>
                {
                    new SelectListItem { Value = "English", Text = "English" },
                    new SelectListItem { Value = "Turkish", Text = "Turkish" }
                },
                LevelSelectList = new List<SelectListItem>
                {
                    new SelectListItem { Value = "MasterThesis", Text = "Master with Thesis" },
                    new SelectListItem { Value = "MasterNonThesis", Text = "Master without Thesis" },
                    new SelectListItem { Value = "PhD", Text = "Doctorate (PhD)" }
                },
                ProgramSelectList = new List<SelectListItem> { new SelectListItem { Value = "", Text = "-- Select Language and Level First --" } }, // Placeholder
                ScholarshipQuotas = new List<ScholarshipQuota>()
            };
            return View(viewModel);
        }
        [HttpGet]
        public async Task<JsonResult> GetFilteredPrograms(string language, string level)
        {
            var query = _context.Programs
                                .Where(p => p.Language == language && p.Level == level);

            bool isTurkish = language?.Equals("Turkish", StringComparison.OrdinalIgnoreCase) == true;

            if (isTurkish)
            {

                query = query.Where(p => !string.IsNullOrEmpty(p.Name) && p.Name != "0")
                             .OrderBy(p => p.Name);
            }
            else
            {
                query = query.Where(p => !string.IsNullOrEmpty(p.NameEn) && p.NameEn != "0")
                             .OrderBy(p => p.NameEn);
            }

            if (isTurkish)
            {
                var programs = await query.Select(p => new 
                {
                    value = p.ProgramId,
                    text = p.Name 
                }).ToListAsync();
                return Json(programs);
            }
            else
            {
                var programs = await query.Select(p => new 
                {
                    value = p.ProgramId,
                    text = p.NameEn 
                }).ToListAsync();
                return Json(programs);
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProgramQuotaCreateViewModel viewModel)
        {
            _logger.LogInformation("--- Create POST action started ---");
            _logger.LogInformation(System.Text.Json.JsonSerializer.Serialize(viewModel));

            if (viewModel.ScholarshipQuotas != null && viewModel.ScholarshipQuotas.Any())
            {
                int i = 0;
                foreach (var sq in viewModel.ScholarshipQuotas)
                {
                    _logger.LogInformation($"Submitted ScholarshipQuota [{i}] Percentage: {sq.ScholarshipPercentage}, Allocated: {sq.AllocatedQuota}");
                    i++;
                }
            } else {
                _logger.LogInformation("Submitted ScholarshipQuotas is null or empty.");
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState is INVALID. Errors:");
                foreach (var key in ModelState.Keys)
                {
                    var state = ModelState[key];
                    if (state.Errors.Any())
                    {
                        _logger.LogWarning($"Key: {key}");
                        foreach (var error in state.Errors)
                        {
                            _logger.LogWarning($"  Error: {error.ErrorMessage}");
                        }
                    }
                }
                
                var currentApplicationPeriods = await _context.ApplicationPeriods
                                                          .Where(ap => ap.IsOpen && !string.IsNullOrEmpty(ap.Name))
                                                          .OrderBy(ap => ap.Name)
                                                          .ToListAsync();
                viewModel.ApplicationPeriodSelectList = new SelectList(currentApplicationPeriods, "Id", "Name", viewModel.ApplicationPeriodId);
                
                viewModel.LanguageSelectList = new List<SelectListItem>
                {
                    new SelectListItem { Value = "English", Text = "English" },
                    new SelectListItem { Value = "Turkish", Text = "Turkish" }
                };
                if (!string.IsNullOrEmpty(viewModel.SelectedLanguage))
                {
                    var langItem = viewModel.LanguageSelectList.FirstOrDefault(lang => lang.Value == viewModel.SelectedLanguage);
                    if (langItem != null) langItem.Selected = true;
                }

                viewModel.LevelSelectList = new List<SelectListItem>
                {
                    new SelectListItem { Value = "MasterThesis", Text = "Master with Thesis" },
                    new SelectListItem { Value = "MasterNonThesis", Text = "Master without Thesis" },
                    new SelectListItem { Value = "PhD", Text = "Doctorate (PhD)" }
                };
                if (!string.IsNullOrEmpty(viewModel.SelectedLevel))
                {
                    var levelItem = viewModel.LevelSelectList.FirstOrDefault(lvl => lvl.Value == viewModel.SelectedLevel);
                    if (levelItem != null) levelItem.Selected = true;
                }

                if (!string.IsNullOrEmpty(viewModel.SelectedLanguage) && !string.IsNullOrEmpty(viewModel.SelectedLevel))
                {
                    var programs = await _context.Programs
                                             .Where(p => p.Language == viewModel.SelectedLanguage && p.Level == viewModel.SelectedLevel && !string.IsNullOrEmpty(p.NameEn) && p.NameEn != "0")
                                             .OrderBy(p => p.NameEn)
                                             .ToListAsync();
                    viewModel.ProgramSelectList = new SelectList(programs, "ProgramId", "NameEn", viewModel.ProgramId);
                }
                else
                {
                    viewModel.ProgramSelectList = new List<SelectListItem> { new SelectListItem { Value = "", Text = "-- Select Language and Level First --" } };
                }

                if (viewModel.ScholarshipQuotas == null) viewModel.ScholarshipQuotas = new List<ScholarshipQuota>();
                
                _logger.LogInformation("Returning View with invalid ModelState.");
                return View(viewModel);
            }

            _logger.LogInformation("ModelState is VALID. Proceeding with saving data.");
            
            var programQuota = new ProgramQuota
            {
                ProgramId = viewModel.ProgramId,
                ApplicationPeriodId = viewModel.ApplicationPeriodId,
                TotalQuota = viewModel.TotalQuota,
                ScholarshipQuotas = viewModel.ScholarshipQuotas ?? new List<ScholarshipQuota>()
            };

            int totalAllocatedForScholarships = programQuota.ScholarshipQuotas.Sum(sq => sq.AllocatedQuota);

            if (totalAllocatedForScholarships != programQuota.TotalQuota)
            {
                ModelState.AddModelError("ScholarshipQuotas", $"The sum of allocated scholarship quotas ({totalAllocatedForScholarships}) must be equal to the total program quota ({programQuota.TotalQuota}).");
                _logger.LogWarning($"Validation Error: Sum of scholarship quotas ({totalAllocatedForScholarships}) is not equal to total program quota ({programQuota.TotalQuota}).");
            }
            else if (programQuota.ScholarshipQuotas.Any(sq => sq.AllocatedQuota < 0) || programQuota.TotalQuota < 0)
            {
                 ModelState.AddModelError("", "Quota values cannot be negative.");
                 _logger.LogWarning("Validation Error: Negative quota value.");
            }

            if (!ModelState.IsValid) 
            {
                _logger.LogWarning("ModelState became INVALID after custom validation. Errors:");
                foreach (var key in ModelState.Keys)
                {
                    var state = ModelState[key];
                    if (state.Errors.Any())
                    {
                        _logger.LogWarning($"Key: {key}");
                        foreach (var error in state.Errors)
                        {
                            _logger.LogWarning($"  Error: {error.ErrorMessage}");
                        }
                    }
                }

                var currentApplicationPeriods = await _context.ApplicationPeriods
                                                          .Where(ap => ap.IsOpen && !string.IsNullOrEmpty(ap.Name))
                                                          .OrderBy(ap => ap.Name)
                                                          .ToListAsync();
                viewModel.ApplicationPeriodSelectList = new SelectList(currentApplicationPeriods, "Id", "Name", viewModel.ApplicationPeriodId);
                
                viewModel.LanguageSelectList = new List<SelectListItem>
                {
                    new SelectListItem { Value = "English", Text = "English" },
                    new SelectListItem { Value = "Turkish", Text = "Turkish" }
                };
                if (!string.IsNullOrEmpty(viewModel.SelectedLanguage))
                {
                    var langItem = viewModel.LanguageSelectList.FirstOrDefault(lang => lang.Value == viewModel.SelectedLanguage);
                    if (langItem != null) langItem.Selected = true;
                }

                viewModel.LevelSelectList = new List<SelectListItem>
                {
                    new SelectListItem { Value = "MasterThesis", Text = "Master with Thesis" },
                    new SelectListItem { Value = "MasterNonThesis", Text = "Master without Thesis" },
                    new SelectListItem { Value = "PhD", Text = "Doctorate (PhD)" }
                };
                if (!string.IsNullOrEmpty(viewModel.SelectedLevel))
                {
                    var levelItem = viewModel.LevelSelectList.FirstOrDefault(lvl => lvl.Value == viewModel.SelectedLevel);
                    if (levelItem != null) levelItem.Selected = true;
                }

                if (!string.IsNullOrEmpty(viewModel.SelectedLanguage) && !string.IsNullOrEmpty(viewModel.SelectedLevel))
                {
                    var programs = await _context.Programs
                                             .Where(p => p.Language == viewModel.SelectedLanguage && p.Level == viewModel.SelectedLevel && !string.IsNullOrEmpty(p.NameEn) && p.NameEn != "0")
                                             .OrderBy(p => p.NameEn)
                                             .ToListAsync();
                    viewModel.ProgramSelectList = new SelectList(programs, "ProgramId", "NameEn", viewModel.ProgramId);
                }
                else
                {
                    viewModel.ProgramSelectList = new List<SelectListItem> { new SelectListItem { Value = "", Text = "-- Select Language and Level First --" } };
                }

                if (viewModel.ScholarshipQuotas == null) viewModel.ScholarshipQuotas = new List<ScholarshipQuota>();
                
                _logger.LogInformation("Returning View with invalid ModelState due to custom validation failure or initial errors.");
                return View(viewModel);
            } 
            
            _context.Add(programQuota);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Program quota created successfully.";
            _logger.LogInformation("Program quota created and saved successfully. Redirecting to Index.");
            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var programQuota = await _context.ProgramQuotas
                .Include(pq => pq.ScholarshipQuotas) 
                .FirstOrDefaultAsync(pq => pq.ProgramQuotaId == id);

            if (programQuota == null)
            {
                return NotFound();
            }
            ViewBag.ApplicationPeriods = new SelectList(await _context.ApplicationPeriods.Where(ap => ap.IsOpen).OrderBy(ap => ap.Name).ToListAsync(), "Id", "Name", programQuota.ApplicationPeriodId);
            ViewBag.Programs = new SelectList(await _context.Programs.OrderBy(p => p.Name).ToListAsync(), "ProgramId", "Name", programQuota.ProgramId);
            return View(programQuota);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProgramQuota programQuota)
        {
            if (id != programQuota.ProgramQuotaId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                if (programQuota.ScholarshipQuotas.Sum(sq => sq.AllocatedQuota) > programQuota.TotalQuota)
                {
                    ModelState.AddModelError("", "The sum of allocated scholarship quotas cannot exceed the total program quota.");
                } 
                else if (programQuota.ScholarshipQuotas.Any(sq => sq.AllocatedQuota < 0) || programQuota.TotalQuota < 0)
                {
                     ModelState.AddModelError("", "Quota values cannot be negative.");
                } 
                else
                {
                    try
                    {
                        var existingProgramQuota = await _context.ProgramQuotas
                            .Include(pq => pq.ScholarshipQuotas)
                            .FirstOrDefaultAsync(pq => pq.ProgramQuotaId == id);

                        if (existingProgramQuota == null) return NotFound();
                        _context.Entry(existingProgramQuota).CurrentValues.SetValues(programQuota);
                        existingProgramQuota.ProgramId = programQuota.ProgramId; 
                        existingProgramQuota.ApplicationPeriodId = programQuota.ApplicationPeriodId;

                        var submittedScholarshipQuotaIds = programQuota.ScholarshipQuotas.Select(s => s.ScholarshipQuotaId).ToList();
                        var scholarshipQuotasToRemove = existingProgramQuota.ScholarshipQuotas
                            .Where(s => !submittedScholarshipQuotaIds.Contains(s.ScholarshipQuotaId) && s.ScholarshipQuotaId != 0)
                            .ToList();
                        _context.ScholarshipQuotas.RemoveRange(scholarshipQuotasToRemove);

                        foreach (var scholarshipQuota in programQuota.ScholarshipQuotas)
                        {
                            var existingScholarship = existingProgramQuota.ScholarshipQuotas
                                .FirstOrDefault(s => s.ScholarshipQuotaId == scholarshipQuota.ScholarshipQuotaId && s.ScholarshipQuotaId != 0);

                            if (existingScholarship != null)
                            {
                                _context.Entry(existingScholarship).CurrentValues.SetValues(scholarshipQuota);
                            }
                            else
                            {
                                scholarshipQuota.ProgramQuotaId = existingProgramQuota.ProgramQuotaId; 
                                existingProgramQuota.ScholarshipQuotas.Add(scholarshipQuota);
                            }
                        }
                        
                        await _context.SaveChangesAsync();
                        TempData["SuccessMessage"] = "Program quota updated successfully.";
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!ProgramQuotaExists(programQuota.ProgramQuotaId))
                        {
                            return NotFound();
                        }
                        else
                        {
                            throw;
                        }
                    }
                    return RedirectToAction(nameof(Index));
                }
            }
            ViewBag.ApplicationPeriods = new SelectList(await _context.ApplicationPeriods.Where(ap => ap.IsOpen).OrderBy(ap => ap.Name).ToListAsync(), "Id", "Name", programQuota.ApplicationPeriodId);
            ViewBag.Programs = new SelectList(await _context.Programs.OrderBy(p => p.Name).ToListAsync(), "ProgramId", "Name", programQuota.ProgramId);
            return View(programQuota);
        }
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var programQuota = await _context.ProgramQuotas
                .Include(pq => pq.Program)
                .Include(pq => pq.ApplicationPeriod)
                .Include(pq => pq.ScholarshipQuotas) 
                .FirstOrDefaultAsync(m => m.ProgramQuotaId == id);

            if (programQuota == null)
            {
                return NotFound();
            }
            bool isAnyScholarshipAwarded = false;
            if (programQuota.ScholarshipQuotas != null && programQuota.ScholarshipQuotas.Any())
            {
                var scholarshipQuotaIds = programQuota.ScholarshipQuotas.Select(sq => sq.ScholarshipQuotaId).ToList();
                isAnyScholarshipAwarded = await _context.ProgramSelections
                    .AnyAsync(ps => ps.AwardedScholarshipQuotaId != null && scholarshipQuotaIds.Contains(ps.AwardedScholarshipQuotaId.Value));
            }

            if (isAnyScholarshipAwarded)
            {
                ViewData["ErrorMessage"] = $"This program quota ('{programQuota.Program?.Name}' for period '{programQuota.ApplicationPeriod?.Name}') cannot be deleted because one or more of its scholarship quotas have been awarded to student applications. Please ensure no students are awarded these scholarship quotas before attempting to delete.";
            }

            return View(programQuota);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var programQuota = await _context.ProgramQuotas
                .Include(pq => pq.ScholarshipQuotas) 
                .FirstOrDefaultAsync(pq => pq.ProgramQuotaId == id);

            if (programQuota == null)
            {
                TempData["ErrorMessage"] = "Program quota not found."; 
                return RedirectToAction(nameof(Index));
            }

            bool isAnyScholarshipAwarded = false;
            if (programQuota.ScholarshipQuotas != null && programQuota.ScholarshipQuotas.Any())
            {
                var scholarshipQuotaIds = programQuota.ScholarshipQuotas.Select(sq => sq.ScholarshipQuotaId).ToList();
                isAnyScholarshipAwarded = await _context.ProgramSelections
                    .AnyAsync(ps => ps.AwardedScholarshipQuotaId != null && scholarshipQuotaIds.Contains(ps.AwardedScholarshipQuotaId.Value));
            }

            if (isAnyScholarshipAwarded)
            {
                TempData["ErrorMessage"] = $"This program quota ('{programQuota.Program?.Name}' for period '{programQuota.ApplicationPeriod?.Name}') cannot be deleted because one or more of its scholarship quotas have been awarded to student applications. Please ensure no students are awarded these scholarship quotas before attempting to delete.";
                return RedirectToAction(nameof(Delete), new { id = id });
            }

            try
            {
                if (programQuota.ScholarshipQuotas != null && programQuota.ScholarshipQuotas.Any())
                {
                    _context.ScholarshipQuotas.RemoveRange(programQuota.ScholarshipQuotas);
                }
                _context.ProgramQuotas.Remove(programQuota);
                
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Program quota and all associated scholarship quotas deleted successfully.";
            }
            catch (DbUpdateException ex)
            {

                _logger.LogError(ex, $"Error deleting program quota with ID {id}. Potentially a concurrent operation or other database constraint.");
                TempData["ErrorMessage"] = "An unexpected error occurred while trying to delete the program quota. Please check logs or try again.";

                return RedirectToAction(nameof(Delete), new { id = id });
            }

            return RedirectToAction(nameof(Index));
        }


        private bool ProgramQuotaExists(int id)
        {
            return _context.ProgramQuotas.Any(e => e.ProgramQuotaId == id);
        }
    }
} 