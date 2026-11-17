using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MediVerify.Data;
using MediVerify.Models;
using MediVerify.Services;
using MediVerify.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MediVerify.Controllers
{
    [Authorize(Policy = "DoctorPolicy")]
    public class PrescriptionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditService _audit;
        private readonly INotificationService _notification;

        public PrescriptionController(ApplicationDbContext context,
                                      UserManager<ApplicationUser> userManager,
                                      IAuditService audit,
                                      INotificationService notification)
        {
            _context = context;
            _userManager = userManager;
            _audit = audit;
            _notification = notification;
        }

        // GET: /Prescription
        public async Task<IActionResult> Index()
        {
            var doctorId = _userManager.GetUserId(User);
            var prescriptions = await _context.Prescriptions
                .Where(p => p.DoctorId == doctorId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
            return View(prescriptions);
        }

        // GET: /Prescription/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Patients = await _context.Patients.ToListAsync();
            ViewBag.Medicines = await _context.Medicines.ToListAsync();
            return View();
        }

        // POST: /Prescription/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PrescriptionCreateViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);
            var doctorId = _userManager.GetUserId(User);
            var prescription = new Prescription
            {
                PatientId = vm.PatientId,
                DoctorId = doctorId,
                PrescriptionNumber = "RX" + DateTime.UtcNow.Ticks,
                Notes = vm.Notes,
                Status = "Draft",
                CreatedAt = DateTime.UtcNow,
                Items = vm.Items.Select(i => new PrescriptionItem
                {
                    MedicineId = i.MedicineId,
                    Quantity = i.Quantity,
                    PrescribedInstructions = i.Instructions,
                    Duration = i.Duration,
                    Notes = i.Notes
                }).ToList()
            };
            _context.Prescriptions.Add(prescription);
            await _context.SaveChangesAsync();
            await _audit.LogAsync(doctorId, "CreatePrescription", "Prescription", prescription.PrescriptionId);
            await _notification.NotifyRoleAsync("Pharmacist", "New Prescription", $"Prescription {prescription.PrescriptionNumber} submitted for review.", $"/Prescription/Details/{prescription.PrescriptionId}");
            return RedirectToAction(nameof(Index));
        }

        // GET: /Prescription/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var prescription = await _context.Prescriptions
                .Include(p => p.Items).ThenInclude(i => i.Medicine)
                .Include(p => p.Patient).ThenInclude(p => p.ApplicationUser)
                .FirstOrDefaultAsync(p => p.PrescriptionId == id);
            if (prescription == null) return NotFound();
            return View(prescription);
        }

        // POST: /Prescription/Submit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int id)
        {
            var prescription = await _context.Prescriptions.FindAsync(id);
            if (prescription == null) return NotFound();
            prescription.Status = "Submitted";
            prescription.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await _audit.LogAsync(prescription.DoctorId, "SubmitPrescription", "Prescription", id);
            await _notification.NotifyRoleAsync("Pharmacist", "Prescription Submitted", $"Prescription {prescription.PrescriptionNumber} awaiting verification.", $"/Prescription/Details/{prescription.PrescriptionId}");
            return RedirectToAction(nameof(Index));
        }
    }
}
