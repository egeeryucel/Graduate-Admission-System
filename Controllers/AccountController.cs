using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using GraduationAdmissionSystem.Models;
using GraduationAdmissionSystem.ViewModels;

namespace GraduationAdmissionSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser 
                { 
                    UserName = model.Email, 
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Role = "Candidate"
                };
                
                var result = await _userManager.CreateAsync(user, model.Password);
                
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Candidate");
                    
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    
                    TempData["SuccessMessage"] = "Registration successful! You can now start your application.";
                    return RedirectToAction("Index", "Home", new { area = "Candidate" });
                }
                
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            
            return View(model);
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt. Please check your email and password.");
                    return View(model);
                }

                if (user.IsDeleted)
                {

                    ModelState.AddModelError(string.Empty, "Your account has been deactivated. Please contact support.");
                    return View(model);
                }
                var result = await _signInManager.PasswordSignInAsync(user.UserName, model.Password, model.RememberMe, lockoutOnFailure: false);
                
                if (result.Succeeded)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    
                    if (roles.Contains("Admin"))
                        return RedirectToAction("Index", "Home", new { area = "Admin" });
                    else if (roles.Contains("Secretary"))
                        return RedirectToAction("Index", "Home", new { area = "Secretary" });
                    else if (roles.Contains("DepartmentChair"))
                        return RedirectToAction("Index", "Home", new { area = "DepartmentChair" });
                    else if (roles.Contains("Agency"))
                        return RedirectToAction("Index", "Home", new { area = "Agency" });
                    else if (roles.Contains("Candidate"))
                        return RedirectToAction("Index", "Home", new { area = "Candidate" });
                    else
                        return RedirectToAction("Index", "Home");
                }
                else
                {
                    if (result.IsLockedOut)
                        ModelState.AddModelError(string.Empty, "Account is locked out.");
                    else if (result.IsNotAllowed)
                        ModelState.AddModelError(string.Empty, "Account is not allowed to login.");
                    else if (result.RequiresTwoFactor)
                        ModelState.AddModelError(string.Empty, "Requires two factor authentication.");
                    else
                        ModelState.AddModelError(string.Empty, "Invalid login attempt. Please check your email and password.");
                }
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home", new { area = "" });
        }

        [HttpGet]
        public IActionResult Application()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ResetAdminPassword()
        {
            var adminUser = await _userManager.FindByEmailAsync("admin@admin.com");
            if (adminUser != null)
            {
                var removePasswordResult = await _userManager.RemovePasswordAsync(adminUser);
                if (removePasswordResult.Succeeded)
                {
                    var addPasswordResult = await _userManager.AddPasswordAsync(adminUser, "Admin123!");
                    if (addPasswordResult.Succeeded)
                    {
                        return Content("Admin password reset successful");
                    }
                    return Content($"Failed to add new password: {string.Join(", ", addPasswordResult.Errors)}");
                }
                return Content($"Failed to remove old password: {string.Join(", ", removePasswordResult.Errors)}");
            }
            return Content("Admin user not found");
        }
    }
} 