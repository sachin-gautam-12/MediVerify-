using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediVerify.Data;
using MediVerify.Models;
using MediVerify.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediVerify.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            if (User.IsInRole("Administrator")) return RedirectToAction(nameof(Admin));
            if (User.IsInRole("Doctor")) return RedirectToAction(nameof(Doctor));
            if (User.IsInRole("Pharmacist")) return RedirectToAction(nameof(Pharmacist));
            if (User.IsInRole("Patient")) return RedirectToAction(nameof(Patient));
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
                LowStockCount = await _context.Inventories.CountAsync(i => i.AvailableQuantity < i.ReorderLevel),
                DispensedToday = await _context.Prescriptions.CountAsync(p => p.Status == "Dispensed" && p.DispensedAt >= DateTime.UtcNow.Date)
            };

            var prescriptions = await _context.Prescriptions.ToListAsync();
            model.PrescriptionsPerDay = BuildDailySeries(prescriptions, 14);
            model.StatusBreakdown = BuildStatusSeries(prescriptions);

            var itemRows = await _context.PrescriptionItems
                .Include(i => i.Medicine)
                .GroupBy(i => i.Medicine.MedicineName)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(6)
                .ToListAsync();
            model.TopMedicines = new ChartSeriesViewModel
            {
                Labels = itemRows.Select(x => x.Name).ToList(),
                Values = itemRows.Select(x => x.Count).ToList()
            };

            model.RecentActivity = await _context.AuditLogs
                .Include(a => a.User)
                .OrderByDescending(a => a.Timestamp)
                .Take(8)
                .ToListAsync();

            model.LowStockItems = await _context.Inventories
                .Include(i => i.Medicine)
                .Where(i => i.AvailableQuantity < i.ReorderLevel)
                .OrderBy(i => i.AvailableQuantity)
                .Take(6)
                .ToListAsync();

            return View(model);
        }

        [Authorize(Policy = "DoctorPolicy")]
        public async Task<IActionResult> Doctor()
        {
            var userId = _userManager.GetUserId(User);
            var prescriptions = await _context.Prescriptions
                .Include(p => p.Patient).ThenInclude(p => p.ApplicationUser)
                .Where(p => p.DoctorId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var patientIds = prescriptions.Select(p => p.PatientId).Distinct().ToList();
            var patients = await _context.Patients
                .Include(p => p.ApplicationUser)
                .Where(p => patientIds.Contains(p.PatientId))
                .ToListAsync();

            var model = new DoctorDashboardViewModel
            {
                MyPatients = patients,
                MyPrescriptions = prescriptions,
                DraftCount = prescriptions.Count(p => p.Status == "Draft"),
                SubmittedCount = prescriptions.Count(p => p.Status == "Submitted"),
                DispensedCount = prescriptions.Count(p => p.Status == "Dispensed"),
                PrescriptionsPerDay = BuildDailySeries(prescriptions, 14),
                StatusBreakdown = BuildStatusSeries(prescriptions)
            };
            return View(model);
        }

        [Authorize(Policy = "PharmacistPolicy")]
        public async Task<IActionResult> Pharmacist()
        {
            var today = DateTime.UtcNow.Date;
            var all = await _context.Prescriptions.ToListAsync();
            var model = new PharmacistDashboardViewModel
            {
                PendingPrescriptions = await _context.Prescriptions
                    .Include(p => p.Patient).ThenInclude(p => p.ApplicationUser)
                    .Include(p => p.Doctor)
                    .Where(p => p.Status == "Submitted")
                    .OrderBy(p => p.CreatedAt)
                    .ToListAsync(),
                LowStockMedicines = await _context.Inventories
                    .Include(i => i.Medicine)
                    .Where(i => i.AvailableQuantity < i.ReorderLevel)
                    .Select(i => i.Medicine)
                    .Distinct()
                    .ToListAsync(),
                VerifiedToday = all.Count(p => p.VerifiedAt >= today),
                DispensedToday = all.Count(p => p.DispensedAt >= today),
                OpenAlerts = await _context.SafetyAlerts.CountAsync(a => a.ReviewStatus != "Resolved"),
                StatusBreakdown = BuildStatusSeries(all),
                LowStockItems = await _context.Inventories
                    .Include(i => i.Medicine)
                    .Where(i => i.AvailableQuantity < i.ReorderLevel)
                    .OrderBy(i => i.AvailableQuantity)
                    .Take(6)
                    .ToListAsync()
            };

            var stock = await _context.Inventories
                .Include(i => i.Medicine)
                .GroupBy(i => i.Medicine.Category ?? "Other")
                .Select(g => new { Category = g.Key, Quantity = g.Sum(i => i.AvailableQuantity) })
                .OrderByDescending(g => g.Quantity)
                .Take(6)
                .ToListAsync();
            model.StockByCategory = new ChartSeriesViewModel
            {
                Labels = stock.Select(s => s.Category).ToList(),
                Values = stock.Select(s => (int)s.Quantity).ToList()
            };
            return View(model);
        }

        [Authorize(Policy = "PatientPolicy")]
        public async Task<IActionResult> Patient()
        {
            var userId = _userManager.GetUserId(User);
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.ApplicationUserId == userId);
            var prescriptions = patient == null
                ? new List<Prescription>()
                : await _context.Prescriptions
                    .Include(p => p.Doctor)
                    .Where(p => p.PatientId == patient.PatientId)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();

            var model = new PatientDashboardViewModel
            {
                Prescriptions = prescriptions,
                ActiveCount = prescriptions.Count(p => p.Status == "Submitted" || p.Status == "Verified"),
                DispensedCount = prescriptions.Count(p => p.Status == "Dispensed"),
                PendingCount = prescriptions.Count(p => p.Status == "Draft"),
                StatusBreakdown = BuildStatusSeries(prescriptions),
                Notifications = await _context.Notifications
                    .Where(n => n.UserId == userId)
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(5)
                    .ToListAsync()
            };
            return View(model);
        }

        private static ChartSeriesViewModel BuildDailySeries(List<Prescription> prescriptions, int days)
        {
            var start = DateTime.UtcNow.Date.AddDays(-(days - 1));
            var labels = Enumerable.Range(0, days).Select(i => start.AddDays(i)).ToList();
            var values = labels.Select(day => prescriptions.Count(p => p.CreatedAt.Date == day)).ToList();
            return new ChartSeriesViewModel
            {
                Labels = labels.Select(d => d.ToString("dd MMM")).ToList(),
                Values = values
            };
        }

        private static ChartSeriesViewModel BuildStatusSeries(List<Prescription> prescriptions)
        {
            var statuses = new[] { "Draft", "Submitted", "Verified", "Rejected", "Dispensed" };
            return new ChartSeriesViewModel
            {
                Labels = statuses.ToList(),
                Values = statuses.Select(s => prescriptions.Count(p => p.Status == s)).ToList()
            };
        }
    }
}
