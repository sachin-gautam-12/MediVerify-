using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MediVerify.Data;
using MediVerify.Models;
using MediVerify.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace MediVerify.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        public DashboardController(ApplicationDbContext context) => _context = context;

        public async Task<IActionResult> Index()
        {
            var role = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
            // fallback if claim not present – use User.IsInRole checks
            if (User.IsInRole("Administrator"))
                return RedirectToAction("Admin");
            if (User.IsInRole("Doctor"))
                return RedirectToAction("Doctor");
            if (User.IsInRole("Pharmacist"))
                return RedirectToAction("Pharmacist");
            if (User.IsInRole("Patient"))
                return RedirectToAction("Patient");
            return RedirectToAction("AccessDenied", "Account");
        }

        [Authorize(Policy = "AdminPolicy")]
        public async Task<IActionResult> Admin()
        {
            var model = new AdminDashboardViewModel
            {
                TotalUsers = await _context.Users.CountAsync(),
                TotalPatients = await _context.Patients.CountAsync(),
                TotalPrescriptions = await _context.Prescriptions.CountAsync(),
                PendingReviews = await _context.Prescriptions.CountAsync(p => p.Status == "Submitted"),
                ActiveMedicines = await _context.Medicines.CountAsync(m => m.IsActive),
                LowStockCount = await _context.Inventories.CountAsync(i => i.AvailableQuantity < i.ReorderLevel)
            };
            return View(model);
        }

        [Authorize(Policy = "DoctorPolicy")]
        public async Task<IActionResult> Doctor()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value;
            var model = new DoctorDashboardViewModel
            {
                MyPatients = await _context.Patients
                    .Where(p => p.ApplicationUserId == userId)
                    .ToListAsync(),
                MyPrescriptions = await _context.Prescriptions
                    .Where(p => p.DoctorId == userId)
                    .ToListAsync()
            };
            return View(model);
        }

        [Authorize(Policy = "PharmacistPolicy")]
        public async Task<IActionResult> Pharmacist()
        {
            var model = new PharmacistDashboardViewModel
            {
                PendingPrescriptions = await _context.Prescriptions
                    .Where(p => p.Status == "Submitted")
                    .ToListAsync(),
                LowStockMedicines = await _context.Inventories
                    .Where(i => i.AvailableQuantity < i.ReorderLevel)
                    .Select(i => i.Medicine)
                    .ToListAsync()
            };
            return View(model);
        }

        [Authorize(Policy = "PatientPolicy")]
        public async Task<IActionResult> Patient()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value;
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.ApplicationUserId == userId);
            var model = new PatientDashboardViewModel
            {
                Prescriptions = patient == null ? null : await _context.Prescriptions
                    .Where(p => p.PatientId == patient.PatientId)
                    .ToListAsync()
            };
            return View(model);
        }
    }
}
