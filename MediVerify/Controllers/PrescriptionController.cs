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
    [Authorize]
    public class PrescriptionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditService _audit;
        private readonly INotificationService _notification;
        private readonly ISafetyService _safety;

        public PrescriptionController(ApplicationDbContext context,
                                      UserManager<ApplicationUser> userManager,
                                      IAuditService audit,
                                      INotificationService notification,
                                      ISafetyService safety)
        {
            _context = context;
            _userManager = userManager;
            _audit = audit;
            _notification = notification;
            _safety = safety;
        }

        // GET: /Prescription
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            IQueryable<Prescription> query = _context.Prescriptions
                .Include(p => p.Patient).ThenInclude(p => p.ApplicationUser)
                .Include(p => p.Doctor);

            if (User.IsInRole("Doctor"))
            {
                query = query.Where(p => p.DoctorId == userId);
            }
            else if (User.IsInRole("Patient"))
            {
                var patient = await GetCurrentPatientAsync(userId);
                if (patient == null) return View(new List<Prescription>());
                query = query.Where(p => p.PatientId == patient.PatientId);
            }
            else if (User.IsInRole("Pharmacist"))
            {
                query = query.Where(p => p.Status != "Draft");
            }

            var prescriptions = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
            return View(prescriptions);
        }

        [Authorize(Policy = "PharmacistPolicy")]
        public async Task<IActionResult> Queue()
        {
            var prescriptions = await _context.Prescriptions
                .Include(p => p.Patient).ThenInclude(p => p.ApplicationUser)
                .Include(p => p.Doctor)
                .Where(p => p.Status == "Submitted")
                .OrderBy(p => p.CreatedAt)
                .ToListAsync();
            return View(prescriptions);
        }

        [Authorize(Policy = "PharmacistPolicy")]
        public async Task<IActionResult> History()
        {
            var prescriptions = await _context.Prescriptions
                .Include(p => p.Patient).ThenInclude(p => p.ApplicationUser)
                .Include(p => p.Doctor)
                .Where(p => p.Status == "Verified" || p.Status == "Rejected" || p.Status == "Dispensed")
                .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
                .ToListAsync();
            return View(prescriptions);
        }

        [Authorize]
        public async Task<IActionResult> Search(string q)
        {
            var userId = _userManager.GetUserId(User);
            IQueryable<Prescription> query = _context.Prescriptions
                .Include(p => p.Patient).ThenInclude(p => p.ApplicationUser)
                .Include(p => p.Doctor);

            if (User.IsInRole("Doctor"))
                query = query.Where(p => p.DoctorId == userId);
            else if (User.IsInRole("Patient"))
            {
                var patient = await GetCurrentPatientAsync(userId);
                query = patient == null ? query.Where(p => false) : query.Where(p => p.PatientId == patient.PatientId);
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(p => p.PrescriptionNumber.Contains(q) ||
                                         (p.Patient.ApplicationUser.FullName != null && p.Patient.ApplicationUser.FullName.Contains(q)));
            }

            var results = await query.OrderByDescending(p => p.CreatedAt).Take(100).ToListAsync();
            ViewData["Query"] = q;
            return View("Index", results);
        }

        // GET: /Prescription/Create
        [Authorize(Policy = "DoctorPolicy")]
        public async Task<IActionResult> Create()
        {
            await LoadCreateLookupsAsync();
            return View(new PrescriptionCreateViewModel { Items = new List<PrescriptionItemViewModel> { new() } });
        }

        // POST: /Prescription/Create
        [Authorize(Policy = "DoctorPolicy")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PrescriptionCreateViewModel vm)
        {
            if (vm.Items == null || vm.Items.Count == 0)
                ModelState.AddModelError(nameof(vm.Items), "Add at least one medicine.");
            if (vm.Items != null && vm.Items.Any(i => i.MedicineId <= 0 || i.Quantity <= 0))
                ModelState.AddModelError(nameof(vm.Items), "Every medicine row needs a medicine and a quantity greater than zero.");
            if (!ModelState.IsValid)
            {
                await LoadCreateLookupsAsync();
                return View(vm);
            }

            var doctorId = _userManager.GetUserId(User);
            var prescription = new Prescription
            {
                PatientId = vm.PatientId,
                DoctorId = doctorId,
                PrescriptionNumber = "RX" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"),
                PrescriptionDate = vm.PrescriptionDate == default ? DateTime.UtcNow : vm.PrescriptionDate,
                ValidUntil = vm.ValidUntil,
                Notes = vm.Notes,
                Status = "Draft",
                CreatedAt = DateTime.UtcNow,
                Items = vm.Items.Select(i => new PrescriptionItem
                {
                    MedicineId = i.MedicineId,
                    Quantity = i.Quantity,
                    Duration = i.Duration,
                    PrescribedInstructions = i.Instructions,
                    Notes = i.Notes
                }).ToList()
            };
            _context.Prescriptions.Add(prescription);
            await _context.SaveChangesAsync();
            await _audit.LogAsync(doctorId, "CreatePrescription", "Prescription", prescription.PrescriptionId);
            TempData["Success"] = $"Prescription {prescription.PrescriptionNumber} saved as draft.";
            return RedirectToAction(nameof(Details), new { id = prescription.PrescriptionId });
        }

        [Authorize(Policy = "DoctorPolicy")]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = _userManager.GetUserId(User);
            var prescription = await _context.Prescriptions
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.PrescriptionId == id);
            if (prescription == null) return NotFound();
            if (prescription.DoctorId != userId) return Forbid();
            if (prescription.Status != "Draft")
            {
                TempData["Error"] = "Only draft prescriptions can be edited.";
                return RedirectToAction(nameof(Details), new { id });
            }

            await LoadCreateLookupsAsync();
            var vm = new PrescriptionCreateViewModel
            {
                PatientId = prescription.PatientId,
                PrescriptionNumber = prescription.PrescriptionNumber,
                PrescriptionDate = prescription.PrescriptionDate,
                ValidUntil = prescription.ValidUntil,
                Notes = prescription.Notes,
                Items = prescription.Items.Select(i => new PrescriptionItemViewModel
                {
                    MedicineId = i.MedicineId,
                    Quantity = i.Quantity,
                    Duration = i.Duration,
                    Instructions = i.PrescribedInstructions,
                    Notes = i.Notes
                }).ToList()
            };
            return View(vm);
        }

        [Authorize(Policy = "DoctorPolicy")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PrescriptionCreateViewModel vm)
        {
            var userId = _userManager.GetUserId(User);
            var prescription = await _context.Prescriptions
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.PrescriptionId == id);
            if (prescription == null) return NotFound();
            if (prescription.DoctorId != userId) return Forbid();
            if (prescription.Status != "Draft")
            {
                TempData["Error"] = "Only draft prescriptions can be edited.";
                return RedirectToAction(nameof(Details), new { id });
            }
            if (vm.Items == null || vm.Items.Count == 0)
                ModelState.AddModelError(nameof(vm.Items), "Add at least one medicine.");
            if (!ModelState.IsValid)
            {
                await LoadCreateLookupsAsync();
                return View(vm);
            }

            prescription.PatientId = vm.PatientId;
            prescription.PrescriptionDate = vm.PrescriptionDate;
            prescription.ValidUntil = vm.ValidUntil;
            prescription.Notes = vm.Notes;
            prescription.UpdatedAt = DateTime.UtcNow;
            _context.PrescriptionItems.RemoveRange(prescription.Items);
            prescription.Items = vm.Items.Select(i => new PrescriptionItem
            {
                MedicineId = i.MedicineId,
                Quantity = i.Quantity,
                Duration = i.Duration,
                PrescribedInstructions = i.Instructions,
                Notes = i.Notes
            }).ToList();
            await _context.SaveChangesAsync();
            await _audit.LogAsync(userId, "EditPrescription", "Prescription", id);
            TempData["Success"] = "Draft updated.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [Authorize(Policy = "DoctorPolicy")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User);
            var prescription = await _context.Prescriptions
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.PrescriptionId == id);
            if (prescription == null) return NotFound();
            if (prescription.DoctorId != userId) return Forbid();
            if (prescription.Status != "Draft")
            {
                TempData["Error"] = "Only draft prescriptions can be deleted.";
                return RedirectToAction(nameof(Details), new { id });
            }

            _context.Prescriptions.Remove(prescription);
            await _context.SaveChangesAsync();
            await _audit.LogAsync(userId, "DeletePrescription", "Prescription", id);
            TempData["Success"] = "Draft deleted.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Prescription/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var prescription = await LoadAccessiblePrescriptionAsync(id);
            if (prescription == null) return NotFound();
            ViewBag.SafetyAlerts = await _context.SafetyAlerts
                .Include(a => a.Medicine)
                .Where(a => a.PrescriptionId == id && a.ReviewStatus != "Resolved")
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
            ViewBag.DispensingRecord = await _context.DispensingRecords
                .Include(d => d.Items).ThenInclude(i => i.Medicine)
                .FirstOrDefaultAsync(d => d.PrescriptionId == id);
            return View(prescription);
        }

        public async Task<IActionResult> Print(int id)
        {
            var prescription = await LoadAccessiblePrescriptionAsync(id);
            if (prescription == null) return NotFound();
            return View(prescription);
        }

        // POST: /Prescription/Submit/5
        [Authorize(Policy = "DoctorPolicy")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int id)
        {
            var userId = _userManager.GetUserId(User);
            var prescription = await _context.Prescriptions
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.PrescriptionId == id);
            if (prescription == null) return NotFound();
            if (prescription.DoctorId != userId) return Forbid();
            if (prescription.Status != "Draft")
            {
                TempData["Error"] = "Only draft prescriptions can be submitted.";
                return RedirectToAction(nameof(Details), new { id });
            }

            prescription.Status = "Submitted";
            prescription.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await _audit.LogAsync(userId, "SubmitPrescription", "Prescription", id);
            await _safety.EvaluateAsync(id, userId);
            await _notification.NotifyRoleAsync("Pharmacist", "Prescription submitted",
                $"Prescription {prescription.PrescriptionNumber} is awaiting verification.", $"/Prescription/Verify/{id}");
            TempData["Success"] = "Prescription submitted for verification.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [Authorize(Policy = "PharmacistPolicy")]
        public async Task<IActionResult> Verify(int id)
        {
            var prescription = await _context.Prescriptions
                .Include(p => p.Items).ThenInclude(i => i.Medicine)
                .Include(p => p.Patient).ThenInclude(p => p.ApplicationUser)
                .Include(p => p.Doctor)
                .FirstOrDefaultAsync(p => p.PrescriptionId == id);
            if (prescription == null) return NotFound();
            ViewBag.SafetyAlerts = await _context.SafetyAlerts
                .Include(a => a.Medicine)
                .Where(a => a.PrescriptionId == id && a.ReviewStatus != "Resolved")
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
            return View(prescription);
        }

        [Authorize(Policy = "PharmacistPolicy")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Verify(int id, bool approve, string reason)
        {
            var pharmacistId = _userManager.GetUserId(User);
            var prescription = await _context.Prescriptions
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.PrescriptionId == id);
            if (prescription == null) return NotFound();
            if (prescription.Status != "Submitted")
            {
                TempData["Error"] = "Only submitted prescriptions can be verified.";
                return RedirectToAction(nameof(Details), new { id });
            }
            if (!approve && string.IsNullOrWhiteSpace(reason))
            {
                TempData["Error"] = "A rejection reason is required.";
                return RedirectToAction(nameof(Verify), new { id });
            }

            prescription.Status = approve ? "Verified" : "Rejected";
            prescription.IsVerified = approve;
            prescription.VerifiedBy = pharmacistId;
            prescription.VerifiedAt = DateTime.UtcNow;
            prescription.ClinicalReviewStatus = approve ? "Approved" : "Rejected";
            prescription.RejectionReason = approve ? null : reason;
            prescription.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await _audit.LogAsync(pharmacistId, approve ? "ApprovePrescription" : "RejectPrescription", "Prescription", id, reason);

            if (approve)
            {
                await _safety.EvaluateAsync(id, pharmacistId);
                await _notification.CreateAsync(prescription.DoctorId, "Prescription verified",
                    $"Prescription {prescription.PrescriptionNumber} was verified and is ready to dispense.", $"/Prescription/Details/{id}");
                await _notification.NotifyRoleAsync("Pharmacist", "Prescription verified",
                    $"Prescription {prescription.PrescriptionNumber} is ready to dispense.", $"/Prescription/Dispense/{id}");
                TempData["Success"] = "Prescription verified. You can now dispense it.";
            }
            else
            {
                await _notification.CreateAsync(prescription.DoctorId, "Prescription rejected",
                    $"Prescription {prescription.PrescriptionNumber} was rejected: {reason}", $"/Prescription/Details/{id}");
                TempData["Success"] = "Prescription rejected and the doctor notified.";
            }
            return RedirectToAction(nameof(Details), new { id });
        }

        [Authorize(Policy = "PharmacistPolicy")]
        public async Task<IActionResult> Dispense(int id)
        {
            var prescription = await _context.Prescriptions
                .Include(p => p.Items).ThenInclude(i => i.Medicine)
                .Include(p => p.Patient).ThenInclude(p => p.ApplicationUser)
                .FirstOrDefaultAsync(p => p.PrescriptionId == id);
            if (prescription == null) return NotFound();
            if (prescription.Status != "Verified")
            {
                TempData["Error"] = "Only verified prescriptions can be dispensed.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var model = new DispenseViewModel { PrescriptionId = id };
            var today = DateTime.UtcNow.Date;
            foreach (var item in prescription.Items)
            {
                var available = await _context.Inventories
                    .Where(i => i.MedicineId == item.MedicineId && i.AvailableQuantity > 0 &&
                                (i.ExpiryDate == null || i.ExpiryDate >= today))
                    .SumAsync(i => (decimal?)i.AvailableQuantity) ?? 0m;
                model.Items.Add(new DispenseItemViewModel
                {
                    PrescriptionItemId = item.PrescriptionItemId,
                    MedicineId = item.MedicineId,
                    RequestedQuantity = item.Quantity,
                    QuantityToDispense = Math.Min(item.Quantity, available)
                });
            }
            ViewBag.Prescription = prescription;
            ViewBag.Available = await GetAvailabilityAsync(prescription.Items.Select(i => i.MedicineId));
            return View(model);
        }

        [Authorize(Policy = "PharmacistPolicy")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Dispense(int id, DispenseViewModel vm)
        {
            var pharmacistId = _userManager.GetUserId(User);
            var prescription = await _context.Prescriptions
                .Include(p => p.Items)
                .Include(p => p.Patient)
                .FirstOrDefaultAsync(p => p.PrescriptionId == id);
            if (prescription == null) return NotFound();
            if (prescription.Status != "Verified")
            {
                TempData["Error"] = "Only verified prescriptions can be dispensed.";
                return RedirectToAction(nameof(Details), new { id });
            }
            if (vm.Items == null || vm.Items.Count == 0)
            {
                TempData["Error"] = "No items to dispense.";
                return RedirectToAction(nameof(Dispense), new { id });
            }

            var today = DateTime.UtcNow.Date;
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var record = new DispensingRecord
                {
                    PrescriptionId = id,
                    PharmacistId = pharmacistId,
                    DispensedAt = DateTime.UtcNow,
                    Items = new List<DispensedItem>()
                };

                foreach (var line in vm.Items.Where(i => i.QuantityToDispense > 0))
                {
                    var prescriptionItem = prescription.Items.FirstOrDefault(i => i.PrescriptionItemId == line.PrescriptionItemId);
                    if (prescriptionItem == null) continue;

                    var remaining = line.QuantityToDispense;
                    var batches = await _context.Inventories
                        .Where(i => i.MedicineId == line.MedicineId && i.AvailableQuantity > 0 &&
                                    (i.ExpiryDate == null || i.ExpiryDate >= today))
                        .OrderBy(i => i.ExpiryDate == null ? DateTime.MaxValue : i.ExpiryDate.Value)
                        .ToListAsync();

                    foreach (var batch in batches)
                    {
                        if (remaining <= 0) break;
                        var take = Math.Min(batch.AvailableQuantity, remaining);
                        batch.AvailableQuantity -= take;
                        batch.LastUpdated = DateTime.UtcNow;
                        remaining -= take;
                    }
                    if (remaining > 0)
                    {
                        await transaction.RollbackAsync();
                        TempData["Error"] = "Insufficient usable stock. Dispensing was cancelled.";
                        return RedirectToAction(nameof(Dispense), new { id });
                    }

                    record.Items.Add(new DispensedItem
                    {
                        MedicineId = line.MedicineId,
                        Quantity = line.QuantityToDispense
                    });
                }

                if (record.Items.Count == 0)
                {
                    await transaction.RollbackAsync();
                    TempData["Error"] = "Enter a quantity to dispense for at least one item.";
                    return RedirectToAction(nameof(Dispense), new { id });
                }

                _context.DispensingRecords.Add(record);
                prescription.Status = "Dispensed";
                prescription.DispensedAt = DateTime.UtcNow;
                prescription.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await _audit.LogAsync(pharmacistId, "DispensePrescription", "Prescription", id);
                await _notification.CreateAsync(prescription.DoctorId, "Prescription dispensed",
                    $"Prescription {prescription.PrescriptionNumber} has been dispensed.", $"/Prescription/Details/{id}");
                if (prescription.Patient != null)
                {
                    await _notification.CreateAsync(prescription.Patient.ApplicationUserId, "Your medicine is ready",
                        $"Prescription {prescription.PrescriptionNumber} has been dispensed.", $"/Prescription/Receipt/{id}");
                }

                TempData["Success"] = "Prescription dispensed successfully.";
                return RedirectToAction(nameof(Receipt), new { id });
            }
            catch
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "Dispensing failed. No stock changes were saved.";
                return RedirectToAction(nameof(Dispense), new { id });
            }
        }

        public async Task<IActionResult> Receipt(int id)
        {
            var prescription = await LoadAccessiblePrescriptionAsync(id);
            if (prescription == null) return NotFound();
            var record = await _context.DispensingRecords
                .Include(d => d.Items).ThenInclude(i => i.Medicine)
                .Include(d => d.Pharmacist)
                .FirstOrDefaultAsync(d => d.PrescriptionId == id);
            if (record == null) return NotFound();
            ViewBag.Prescription = prescription;
            return View(record);
        }

        private async Task<Patient> GetCurrentPatientAsync(string userId)
        {
            return await _context.Patients.FirstOrDefaultAsync(p => p.ApplicationUserId == userId);
        }

        private async Task<Prescription> LoadAccessiblePrescriptionAsync(int id)
        {
            var prescription = await _context.Prescriptions
                .Include(p => p.Items).ThenInclude(i => i.Medicine)
                .Include(p => p.Patient).ThenInclude(p => p.ApplicationUser)
                .Include(p => p.Doctor)
                .FirstOrDefaultAsync(p => p.PrescriptionId == id);
            if (prescription == null) return null;

            var userId = _userManager.GetUserId(User);
            if (User.IsInRole("Administrator") || User.IsInRole("Pharmacist")) return prescription;
            if (User.IsInRole("Doctor")) return prescription.DoctorId == userId ? prescription : null;
            if (User.IsInRole("Patient"))
            {
                var patient = await GetCurrentPatientAsync(userId);
                return patient != null && prescription.PatientId == patient.PatientId ? prescription : null;
            }
            return null;
        }

        private async Task LoadCreateLookupsAsync()
        {
            ViewBag.Patients = await _context.Patients
                .Include(p => p.ApplicationUser)
                .OrderBy(p => p.ApplicationUser.FullName)
                .ToListAsync();
            ViewBag.Medicines = await _context.Medicines
                .Where(m => m.IsActive)
                .OrderBy(m => m.MedicineName)
                .ToListAsync();
        }

        private async Task<Dictionary<int, decimal>> GetAvailabilityAsync(IEnumerable<int> medicineIds)
        {
            var today = DateTime.UtcNow.Date;
            var ids = medicineIds.Distinct().ToList();
            var rows = await _context.Inventories
                .Where(i => ids.Contains(i.MedicineId) && i.AvailableQuantity > 0 &&
                            (i.ExpiryDate == null || i.ExpiryDate >= today))
                .GroupBy(i => i.MedicineId)
                .Select(g => new { MedicineId = g.Key, Quantity = g.Sum(x => x.AvailableQuantity) })
                .ToListAsync();
            return rows.ToDictionary(r => r.MedicineId, r => r.Quantity);
        }
    }
}
