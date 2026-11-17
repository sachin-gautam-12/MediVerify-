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
    [Authorize(Roles = "Patient")]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditService _audit;

        public ProfileController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IAuditService audit)
        {
            _context = context;
            _userManager = userManager;
            _audit = audit;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.ApplicationUserId == userId);
            if (patient == null) return NotFound();
            var user = await _userManager.FindByIdAsync(userId);
            return View(new PatientProfileViewModel
            {
                Email = user?.Email,
                FullName = user?.FullName,
                PhoneNumber = user?.PhoneNumber,
                DateOfBirth = patient.DateOfBirth,
                Gender = patient.Gender,
                Address = patient.Address,
                EmergencyContact = patient.EmergencyContact
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(PatientProfileViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);
            var userId = _userManager.GetUserId(User);
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.ApplicationUserId == userId);
            var user = await _userManager.FindByIdAsync(userId);
            if (patient == null || user == null) return NotFound();

            user.FullName = vm.FullName;
            user.PhoneNumber = vm.PhoneNumber;
            patient.DateOfBirth = vm.DateOfBirth;
            patient.Gender = vm.Gender;
            patient.Address = vm.Address;
            patient.EmergencyContact = vm.EmergencyContact;

            await _userManager.UpdateAsync(user);
            await _context.SaveChangesAsync();
            await _audit.LogAsync(userId, "UpdateProfile", "Patient", patient.PatientId);
            TempData["Success"] = "Profile updated.";
            return RedirectToAction(nameof(Index));
        }
    }
}
