using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediVerify.Data;
using MediVerify.Models;
using Microsoft.EntityFrameworkCore;

namespace MediVerify.Services
{
    public class SafetyService : ISafetyService
    {
        private readonly ApplicationDbContext _context;

        public SafetyService(ApplicationDbContext context) => _context = context;

        public async Task<List<SafetyAlert>> EvaluateAsync(int prescriptionId, string reviewedBy = null)
        {
            var prescription = await _context.Prescriptions
                .Include(p => p.Items).ThenInclude(i => i.Medicine)
                .FirstOrDefaultAsync(p => p.PrescriptionId == prescriptionId);

            if (prescription == null) return new List<SafetyAlert>();

            var existing = await _context.SafetyAlerts
                .Where(a => a.PrescriptionId == prescriptionId)
                .ToListAsync();

            var created = new List<SafetyAlert>();

            void AddAlert(int medicineId, string type, string description, string severity)
            {
                if (existing.Any(a => a.MedicineId == medicineId && a.AlertType == type && a.Description == description))
                    return;
                var alert = new SafetyAlert
                {
                    PrescriptionId = prescriptionId,
                    MedicineId = medicineId,
                    AlertType = type,
                    Description = description,
                    Severity = severity,
                    ReviewStatus = "Pending",
                    CreatedAt = DateTime.UtcNow
                };
                _context.SafetyAlerts.Add(alert);
                created.Add(alert);
            }

            var items = prescription.Items?.ToList() ?? new List<PrescriptionItem>();

            // 1. Drug interactions
            for (var i = 0; i < items.Count; i++)
            {
                for (var j = i + 1; j < items.Count; j++)
                {
                    var a = items[i].MedicineId;
                    var b = items[j].MedicineId;
                    if (a == b) continue;
                    var interaction = await _context.DrugInteractions
                        .FirstOrDefaultAsync(d => (d.MedicineAId == a && d.MedicineBId == b) ||
                                                  (d.MedicineAId == b && d.MedicineBId == a));
                    if (interaction != null)
                    {
                        var nameA = items[i].Medicine?.MedicineName ?? $"#{a}";
                        var nameB = items[j].Medicine?.MedicineName ?? $"#{b}";
                        AddAlert(a, "Drug Interaction",
                            $"{nameA} + {nameB}: {interaction.Description}", interaction.Severity);
                    }
                }
            }

            // 2. Duplicate therapy (same medicine or same generic)
            var duplicateGroups = items.GroupBy(i => i.MedicineId).Where(g => g.Count() > 1);
            foreach (var group in duplicateGroups)
            {
                var item = group.First();
                AddAlert(item.MedicineId, "Duplicate Therapy",
                    $"{item.Medicine?.MedicineName ?? "Medicine"} is prescribed {group.Count()} times in this prescription.",
                    "Moderate");
            }
            var genericGroups = items
                .Where(i => !string.IsNullOrWhiteSpace(i.Medicine?.GenericName))
                .GroupBy(i => i.Medicine.GenericName.Trim().ToLowerInvariant())
                .Where(g => g.Select(i => i.MedicineId).Distinct().Count() > 1);
            foreach (var group in genericGroups)
            {
                var first = group.First();
                AddAlert(first.MedicineId, "Duplicate Therapy",
                    $"Multiple medicines share the generic name '{first.Medicine.GenericName}'.",
                    "Moderate");
            }

            // 3. Low / out of stock and expired stock
            var today = DateTime.UtcNow.Date;
            foreach (var item in items)
            {
                var batches = await _context.Inventories
                    .Where(i => i.MedicineId == item.MedicineId)
                    .ToListAsync();
                var usable = batches.Where(b => b.ExpiryDate == null || b.ExpiryDate.Value.Date >= today).Sum(b => b.AvailableQuantity);
                if (usable <= 0)
                {
                    AddAlert(item.MedicineId, "Out Of Stock",
                        $"{item.Medicine?.MedicineName ?? "Medicine"} has no usable stock available.", "High");
                }
                else if (usable < item.Quantity)
                {
                    AddAlert(item.MedicineId, "Low Stock",
                        $"{item.Medicine?.MedicineName ?? "Medicine"}: requested {item.Quantity}, only {usable} usable in stock.", "High");
                }

                var hasExpired = batches.Any(b => b.AvailableQuantity > 0 && b.ExpiryDate.HasValue && b.ExpiryDate.Value.Date < today);
                if (hasExpired)
                {
                    AddAlert(item.MedicineId, "Expired Stock",
                        $"{item.Medicine?.MedicineName ?? "Medicine"} has expired batches in inventory.", "Moderate");
                }

                // 4. Excessive quantity / duration
                if (item.Quantity > 500)
                {
                    AddAlert(item.MedicineId, "Excessive Quantity",
                        $"{item.Medicine?.MedicineName ?? "Medicine"}: quantity {item.Quantity} exceeds the 500-unit review threshold.", "Moderate");
                }
                if (item.Duration > 180)
                {
                    AddAlert(item.MedicineId, "Excessive Duration",
                        $"{item.Medicine?.MedicineName ?? "Medicine"}: duration {item.Duration} days exceeds the 180-day review threshold.", "Moderate");
                }
            }

            // 5. Expired prescription
            if (prescription.ValidUntil.HasValue && prescription.ValidUntil.Value.Date < today)
            {
                var firstMedicine = items.FirstOrDefault()?.MedicineId ?? 0;
                AddAlert(firstMedicine, "Expired Prescription",
                    $"Prescription expired on {prescription.ValidUntil:yyyy-MM-dd}.", "High");
            }

            if (created.Count > 0)
                await _context.SaveChangesAsync();

            return created;
        }

        public async Task<bool> AcknowledgeAsync(int alertId, string userId)
        {
            var alert = await _context.SafetyAlerts.FindAsync(alertId);
            if (alert == null) return false;
            alert.ReviewStatus = "Acknowledged";
            alert.ReviewedBy = userId;
            alert.ReviewedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ResolveAsync(int alertId, string userId)
        {
            var alert = await _context.SafetyAlerts.FindAsync(alertId);
            if (alert == null) return false;
            alert.ReviewStatus = "Resolved";
            alert.ReviewedBy = userId;
            alert.ReviewedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
