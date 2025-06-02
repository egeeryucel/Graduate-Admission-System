using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using GraduationAdmissionSystem.Models;
using GraduationAdmissionSystem.ViewModels;
using System.Text;
using GraduationAdmissionSystem.Data;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;

namespace GraduationAdmissionSystem.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class HomeController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            ILogger<HomeController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> CreateUser()
        {
            var allRoles = await _roleManager.Roles
                                        .Where(r => r.Name != "Admin" && r.Name != "Candidate")
                                        .OrderBy(r => r.Name)
                                        .ToListAsync();
          
            var distinctRolesByName = allRoles
                                        .GroupBy(r => r.Name)
                                        .Select(g => g.First())
                                        .ToList();
            ViewBag.Roles = new SelectList(distinctRolesByName, "Name", "Name");

            var departments = await _context.Departments.OrderBy(d => d.Name).ToListAsync();
            ViewBag.AllDepartments = departments; 

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model, List<int> selectedDepartmentIds)
        {
            var allRoles = await _roleManager.Roles
                                           .Where(r => r.Name != "Admin" && r.Name != "Candidate")
                                           .OrderBy(r => r.Name)
                                           .ToListAsync();
            var distinctRolesByName = allRoles
                                        .GroupBy(r => r.Name)
                                        .Select(g => g.First())
                                        .ToList();
            ViewBag.Roles = new SelectList(distinctRolesByName, "Name", "Name", model.Role);
            
            var departments = await _context.Departments.OrderBy(d => d.Name).ToListAsync();
            ViewBag.AllDepartments = departments; 

            if (ModelState.IsValid)
            {
                if (model.Role == "DepartmentChair" && (selectedDepartmentIds == null || !selectedDepartmentIds.Any()))
                {
                    ModelState.AddModelError(nameof(selectedDepartmentIds), "Please select at least one department for the Department Chair.");
                    return View(model);
                }

                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Role = model.Role,
                    IsApproved = true,
                    IsActive = true,
                    IsDeleted = false,
                    RegistrationDate = DateTime.UtcNow,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, model.Role);
                    _logger.LogInformation($"User {user.Email} created with role {model.Role}.");

                    if (model.Role == "DepartmentChair" && selectedDepartmentIds != null && selectedDepartmentIds.Any())
                    {
                        var assignedDepartments = await _context.Departments
                                                              .Where(d => selectedDepartmentIds.Contains(d.DepartmentId))
                                                              .ToListAsync();
                        
                        user.ManagedDepartments ??= new List<Department>(); 
                        
                        foreach (var dept in assignedDepartments)
                        {
                            user.ManagedDepartments.Add(dept);
                             _logger.LogInformation($"Assigning Department ID {dept.DepartmentId} ({dept.Name}) to user {user.Email}.");
                        }
                        var updateResult = await _userManager.UpdateAsync(user); 
                        if (!updateResult.Succeeded)
                        {
                             _logger.LogError($"Failed to assign departments to user {user.Email}.");
                             TempData["Error"] = "User created, but failed to assign departments.";
                             return RedirectToAction(nameof(Index)); 
                        }
                         _logger.LogInformation($"Successfully assigned {assignedDepartments.Count} departments to user {user.Email}.");
                    }

                    return RedirectToAction(nameof(Index));
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

         
            return View(model);
        }

        private string GenerateRandomPassword()
        {
            var random = new Random();
            var chars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            var password = new StringBuilder();
            
            
            for (int i = 0; i < 12; i++)
            {
                password.Append(chars[random.Next(chars.Length)]);
            }

            return password.ToString();
        }

        [HttpPost]
        public async Task<IActionResult> GetAvailablePrograms(string language, string faculty)
        {
            if (string.IsNullOrEmpty(language) || string.IsNullOrEmpty(faculty))
            {
                return Json(new List<string>()); 
            }

            var programs = await _context.Programs 
                .Where(p => p.Language == language && p.FacultyInstitute == faculty)
                .Select(p => p.Name) 
                .Distinct()
                .ToListAsync();

            return Json(programs); 
        }

        
        public async Task<IActionResult> ManageUsers(bool showDeleted = false)
        {
            _logger.LogInformation($"ManageUsers page requested. ShowDeleted: {showDeleted}");
            IQueryable<ApplicationUser> usersQuery = _userManager.Users
                                        .Where(u => u.Role != "Student");

            if (showDeleted)
            {
                usersQuery = usersQuery.Where(u => u.IsDeleted == true);
                ViewData["Title"] = "Manage Deleted Users";
                ViewData["ShowDeleted"] = true;
            }
            else
            {
                usersQuery = usersQuery.Where(u => u.IsDeleted == false);
                ViewData["Title"] = "Manage Active Users";
                ViewData["ShowDeleted"] = false;
            }

            var users = await usersQuery.ToListAsync();
            var usersWithDetails = new List<ApplicationUser>();
            foreach (var user in users)
            {
                var userWithDepts = await _context.Users
                                                  .Include(u => u.ManagedDepartments)
                                                  .FirstOrDefaultAsync(u => u.Id == user.Id);
                if (userWithDepts != null)
                {
                    usersWithDetails.Add(userWithDepts);
                }
                else
                {
                    usersWithDetails.Add(user);
                }
            }

            _logger.LogInformation($"Found {usersWithDetails.Count} users to display based on filter (ShowDeleted: {showDeleted}).");
            return View(usersWithDetails);
        }

       
        public async Task<IActionResult> EditUser(string id)
        {
            
            _logger.LogInformation($"EditUser page requested for user ID: {id}");
            if (id == null)
            {
                _logger.LogWarning("EditUser called with null ID.");
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                _logger.LogWarning($"User not found for ID: {id} in EditUser.");
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(ManageUsers));
            }

            
            var userRoles = await _userManager.GetRolesAsync(user);
            var currentRole = userRoles.FirstOrDefault(); 

            var allDepartments = await _context.Departments.OrderBy(d => d.Name).ToListAsync();
            var allRoles = await _roleManager.Roles
                                    .Where(r => r.Name != "Student") 
                                    .OrderBy(r => r.Name)
                                    .Select(r => r.Name)
                                    .ToListAsync(); 
            
            var managedDepartmentIds = new List<int>();
            if (currentRole == "DepartmentChair")
            {
                 var userWithDepartments = await _context.Users
                                            .Include(u => u.ManagedDepartments)
                                            .FirstOrDefaultAsync(u => u.Id == user.Id);
                if(userWithDepartments != null)
                {
                    managedDepartmentIds = userWithDepartments.ManagedDepartments.Select(d => d.DepartmentId).ToList();
                }
            }

            var model = new EditUserViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                CurrentRole = currentRole,
                SelectedDepartmentIds = managedDepartmentIds,
                AllDepartments = allDepartments.Select(d => new SelectListItem { Value = d.DepartmentId.ToString(), Text = d.Name }).ToList(),
                AllRoles = new SelectList(allRoles, currentRole) 
            };

            return View(model);
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            _logger.LogInformation($"EditUser POST action called for user ID: {model.Id}");
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("EditUser POST model state is invalid.");
                model.AllDepartments = (await _context.Departments.OrderBy(d => d.Name).ToListAsync())
                                      .Select(d => new SelectListItem { Value = d.DepartmentId.ToString(), Text = d.Name }).ToList();
                model.AllRoles = new SelectList(await _roleManager.Roles.Where(r => r.Name != "Student").OrderBy(r => r.Name).Select(r=>r.Name).ToListAsync(), model.NewRoleAssignment ?? model.CurrentRole);
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
            {
                _logger.LogWarning($"User not found for ID: {model.Id} during EditUser POST.");
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(ManageUsers));
            }

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;

           
            var currentRolesBeforeUpdate = await _userManager.GetRolesAsync(user);
            var currentRoleForLogicBeforeUpdate = currentRolesBeforeUpdate.FirstOrDefault();
            var newRoleToAssignBeforeUpdate = model.NewRoleAssignment ?? currentRoleForLogicBeforeUpdate;
            user.Role = newRoleToAssignBeforeUpdate;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                _logger.LogError($"Failed to update user basic info for {user.Email}. Errors: {string.Join(", ", updateResult.Errors.Select(e => e.Description))}");
                AddModelErrors(updateResult);
                model.AllDepartments = (await _context.Departments.OrderBy(d => d.Name).ToListAsync())
                                      .Select(d => new SelectListItem { Value = d.DepartmentId.ToString(), Text = d.Name }).ToList();
                model.AllRoles = new SelectList(await _roleManager.Roles.Where(r => r.Name != "Student").OrderBy(r => r.Name).Select(r => r.Name).ToListAsync(), model.NewRoleAssignment ?? model.CurrentRole);
                return View(model);
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            var currentRoleForLogic = currentRoles.FirstOrDefault();
            var newRoleToAssign = model.NewRoleAssignment ?? currentRoleForLogic; 

            if (!string.IsNullOrEmpty(currentRoleForLogic) && currentRoleForLogic != newRoleToAssign)
            {
                var removeResult = await _userManager.RemoveFromRoleAsync(user, currentRoleForLogic);
                if (!removeResult.Succeeded)
                {
                    _logger.LogError($"Failed to remove user from role {currentRoleForLogic}. Errors: {string.Join(", ", removeResult.Errors.Select(e => e.Description))}");
                    AddModelErrors(removeResult);
                    model.AllDepartments = (await _context.Departments.OrderBy(d => d.Name).ToListAsync()).Select(d => new SelectListItem { Value = d.DepartmentId.ToString(), Text = d.Name }).ToList();
                    model.AllRoles = new SelectList(await _roleManager.Roles.Where(r => r.Name != "Student").OrderBy(r => r.Name).Select(r => r.Name).ToListAsync(), newRoleToAssign);
                    return View(model);
                }
            }
            
            if (!string.IsNullOrEmpty(newRoleToAssign) && (string.IsNullOrEmpty(currentRoleForLogic) || currentRoleForLogic != newRoleToAssign))
            {
                var addResult = await _userManager.AddToRoleAsync(user, newRoleToAssign);
                 if (!addResult.Succeeded)
                {
                    _logger.LogError($"Failed to add user to role {newRoleToAssign}. Errors: {string.Join(", ", addResult.Errors.Select(e => e.Description))}");
                    AddModelErrors(addResult);
                    model.AllDepartments = (await _context.Departments.OrderBy(d => d.Name).ToListAsync()).Select(d => new SelectListItem { Value = d.DepartmentId.ToString(), Text = d.Name }).ToList();
                    model.AllRoles = new SelectList(await _roleManager.Roles.Where(r => r.Name != "Student").OrderBy(r => r.Name).Select(r => r.Name).ToListAsync(), newRoleToAssign);
                    return View(model);
                }
            }
            
            
            var userForDepartmentUpdate = await _context.Users
                                          .Include(u => u.ManagedDepartments)
                                          .FirstOrDefaultAsync(u => u.Id == user.Id);
            
            if (userForDepartmentUpdate != null) 
            {
                if (newRoleToAssign == "DepartmentChair")
                {
                    userForDepartmentUpdate.ManagedDepartments.Clear(); 
                    if (model.SelectedDepartmentIds != null && model.SelectedDepartmentIds.Any())
                    {
                        var departmentsToAssign = await _context.Departments
                                                              .Where(d => model.SelectedDepartmentIds.Contains(d.DepartmentId))
                                                              .ToListAsync();
                        foreach (var dept in departmentsToAssign)
                        {
                            userForDepartmentUpdate.ManagedDepartments.Add(dept);
                        }
                    }
                }
                else 
                {
                    userForDepartmentUpdate.ManagedDepartments.Clear();
                }
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "User details updated successfully.";
            _logger.LogInformation($"User {user.Email} updated successfully.");
            return RedirectToAction(nameof(ManageUsers));
        }

        private void AddModelErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["Error"] = "User ID cannot be null or empty.";
                return RedirectToAction(nameof(ManageUsers));
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(ManageUsers));
            }

            
            if (user.Email.Equals("admin@admin.com", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "The main admin account cannot be deleted.";
                return RedirectToAction(nameof(ManageUsers));
            }

            try
            {
                user.IsDeleted = true;
                user.IsActive = false; 

                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    TempData["Success"] = "User marked as deleted and deactivated successfully.";
                    _logger.LogInformation($"User {user.Email} marked as deleted and deactivated.");
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    TempData["Error"] = $"Error updating user: {errors}";
                    _logger.LogError($"Error marking user {user.Email} as deleted: {errors}");
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while marking the user as deleted.";
                _logger.LogError(ex, $"Exception occurred while marking user {user.Email} as deleted.");
            }

            return RedirectToAction(nameof(ManageUsers));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReactivateUser(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["Error"] = "User ID cannot be null or empty.";
                return RedirectToAction(nameof(ManageUsers));
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(ManageUsers));
            }

            
            if (!user.IsDeleted)
            {
                TempData["Warning"] = "This user is not currently marked as deleted.";
                return RedirectToAction(nameof(ManageUsers));
            }

            try
            {
                user.IsDeleted = false;
                user.IsActive = true;
               

                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    TempData["Success"] = $"User {user.Email} reactivated successfully.";
                    _logger.LogInformation($"User {user.Email} reactivated.");
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    TempData["Error"] = $"Error reactivating user: {errors}";
                    _logger.LogError($"Error reactivating user {user.Email}: {errors}");
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while reactivating the user.";
                _logger.LogError(ex, $"Exception occurred while reactivating user {user.Email}.");
            }

          
            return RedirectToAction(nameof(ManageUsers), new { showDeleted = true });
        }

       
        [HttpGet] 
        public async Task<IActionResult> ActivateExistingInactiveUsers()
        {
            _logger.LogInformation("Attempting to activate existing non-deleted, inactive users.");
            var usersToActivate = await _userManager.Users
                                        .Where(u => !u.IsDeleted && !u.IsActive)
                                        .ToListAsync();

            if (!usersToActivate.Any())
            {
                TempData["Info"] = "No inactive users found to activate.";
                _logger.LogInformation("No inactive users found to activate.");
                return RedirectToAction(nameof(ManageUsers));
            }

            int successCount = 0;
            int failureCount = 0;
            List<string> errorMessages = new List<string>();

            foreach (var user in usersToActivate)
            {
                user.IsActive = true;
                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    successCount++;
                    _logger.LogInformation($"User {user.Email} activated successfully.");
                }
                else
                {
                    failureCount++;
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogError($"Failed to activate user {user.Email}. Errors: {errors}");
                    errorMessages.Add($"Failed for {user.Email}: {errors}");
                }
            }

            StringBuilder summaryMessage = new StringBuilder();
            summaryMessage.AppendLine($"{successCount} user(s) activated successfully.");
            if (failureCount > 0)
            {
                summaryMessage.AppendLine($"{failureCount} user(s) failed to activate.");
                summaryMessage.AppendLine("Error details:");
                errorMessages.ForEach(e => summaryMessage.AppendLine(e));
                TempData["Error"] = summaryMessage.ToString();
            }
            else
            {
                TempData["Success"] = summaryMessage.ToString();
            }
            
            return RedirectToAction(nameof(ManageUsers));
        }
    }
} 