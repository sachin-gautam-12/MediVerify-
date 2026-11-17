using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediVerify.Data;
using MediVerify.Models;
using MediVerify.Services;
using MediVerify.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediVerify.Controllers
{
    [Authorize(Policy = "AdminPolicy")]
    public class UserAdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _audit;

        public UserAdminController(UserManager<ApplicationUser> userManager,
                                   RoleManager<IdentityRole> roleManager,
                                   ApplicationDbContext context,
                                   IAuditService audit)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _audit = audit;
        }

        public async Task<IActionResult> Index(string q)
        {
            var query = _userManager.Users.AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(u => u.Email.Contains(q) || (u.FullName != null && u.FullName.Contains(q)));
            }
            var users = await query.OrderBy(u => u.FullName).ToListAsync();
            var rows = new List<UserRowViewModel>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                rows.Add(new UserRowViewModel { User = user, Role = roles.FirstOrDefault() ?? "—" });
            }
            ViewBag.Query = q;
            return View(rows);
        }

        public IActionResult Create()
        {
            return View(new UserCreateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserCreateViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);
            if (await _userManager.FindByEmailAsync(vm.Email) != null)
            {
                ModelState.AddModelError(nameof(vm.Email), "A user with this email already exists.");
                return View(vm);
            }
            if (!await _roleManager.RoleExistsAsync(vm.Role))
            {
                ModelState.AddModelError(nameof(vm.Role), "Unknown role.");
                return View(vm);
            }

            var user = new ApplicationUser
            {
                UserName = vm.Email,
                Email = vm.Email,
                FullName = vm.FullName,
                PhoneNumber = vm.PhoneNumber,
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            var result = await _userManager.CreateAsync(user, vm.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
                return View(vm);
            }
            await _userManager.AddToRoleAsync(user, vm.Role);
            if (vm.Role == "Patient")
            {
                _context.Patients.Add(new Patient
                {
                    ApplicationUserId = user.Id,
                    DateOfBirth = vm.DateOfBirth ?? new DateTime(1990, 1, 1),
                    Gender = string.IsNullOrWhiteSpace(vm.Gender) ? "Unspecified" : vm.Gender,
                    Address = "",
                    EmergencyContact = ""
                });
                await _context.SaveChangesAsync();
            }
            await _audit.LogAsync(_userManager.GetUserId(User), "CreateUser", "ApplicationUser", null, $"{vm.Email} as {vm.Role}");
            TempData["Success"] = $"User {vm.Email} created as {vm.Role}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            if (user.Id == _userManager.GetUserId(User))
            {
                TempData["Error"] = "You cannot deactivate your own account.";
                return RedirectToAction(nameof(Index));
            }
            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);
            await _audit.LogAsync(_userManager.GetUserId(User), user.IsActive ? "ActivateUser" : "DeactivateUser", "ApplicationUser", null, user.Email);
            TempData["Success"] = $"{(user.IsActive ? "Activated" : "Deactivated")} {user.Email}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var temp = "Temp#" + Guid.NewGuid().ToString("N").Substring(0, 6);
            var result = await _userManager.ResetPasswordAsync(user, token, temp);
            if (result.Succeeded)
            {
                await _audit.LogAsync(_userManager.GetUserId(User), "ResetPassword", "ApplicationUser", null, user.Email);
                TempData["Success"] = $"Temporary password for {user.Email}: {temp}";
            }
            else
            {
                TempData["Error"] = "Password reset failed.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(string id, string role)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            if (!await _roleManager.RoleExistsAsync(role))
            {
                TempData["Error"] = "Unknown role.";
                return RedirectToAction(nameof(Index));
            }
            var roles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, roles);
            await _userManager.AddToRoleAsync(user, role);
            if (role == "Patient" && !await _context.Patients.AnyAsync(p => p.ApplicationUserId == user.Id))
            {
                _context.Patients.Add(new Patient
                {
                    ApplicationUserId = user.Id,
                    DateOfBirth = new DateTime(1990, 1, 1),
                    Gender = "Unspecified"
                });
                await _context.SaveChangesAsync();
            }
            await _audit.LogAsync(_userManager.GetUserId(User), "ChangeRole", "ApplicationUser", null, $"{user.Email} -> {role}");
            TempData["Success"] = $"{user.Email} is now {role}.";
            return RedirectToAction(nameof(Index));
        }
    }
}
