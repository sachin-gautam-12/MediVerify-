using System;
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
    [Authorize(Roles = "Administrator,Pharmacist")]
    public class InventoryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditService _audit;

        public InventoryController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IAuditService audit)
        {
            _context = context;
            _userManager = userManager;
            _audit = audit;
        }

        public async Task<IActionResult> Index(string q, string status)
        {
            var query = _context.Inventories.Include(i => i.Medicine).AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(i => i.Medicine.MedicineName.Contains(q) ||
                                         (i.BatchNumber != null && i.BatchNumber.Contains(q)));
            }
            var today = DateTime.UtcNow.Date;
            if (status == "low")
                query = query.Where(i => i.AvailableQuantity > 0 && i.AvailableQuantity < i.ReorderLevel);
            else if (status == "out")
                query = query.Where(i => i.AvailableQuantity <= 0);
            else if (status == "expired")
                query = query.Where(i => i.ExpiryDate != null && i.ExpiryDate < today);

            var rows = await query
                .OrderBy(i => i.Medicine.MedicineName)
                .ThenBy(i => i.ExpiryDate)
                .ToListAsync();
            ViewBag.Query = q;
            ViewBag.Status = status;
            return View(rows);
        }

        public async Task<IActionResult> AddStock()
        {
            ViewBag.Medicines = await _context.Medicines.Where(m => m.IsActive).OrderBy(m => m.MedicineName).ToListAsync();
            return View(new InventoryViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStock(InventoryViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Medicines = await _context.Medicines.Where(m => m.IsActive).OrderBy(m => m.MedicineName).ToListAsync();
                return View(vm);
            }
            var medicine = await _context.Medicines.FindAsync(vm.MedicineId);
            if (medicine == null)
            {
                ModelState.AddModelError(nameof(vm.MedicineId), "Select a valid medicine.");
                ViewBag.Medicines = await _context.Medicines.Where(m => m.IsActive).OrderBy(m => m.MedicineName).ToListAsync();
                return View(vm);
            }

            var inventory = new Inventory
            {
                MedicineId = vm.MedicineId,
                AvailableQuantity = vm.AvailableQuantity,
                ReorderLevel = vm.ReorderLevel,
                BatchNumber = vm.BatchNumber,
                ExpiryDate = vm.ExpiryDate,
                LastUpdated = DateTime.UtcNow
            };
            _context.Inventories.Add(inventory);
            await _context.SaveChangesAsync();
            await _audit.LogAsync(_userManager.GetUserId(User), "AddStock", "Inventory", inventory.InventoryId,
                $"{medicine.MedicineName} +{vm.AvailableQuantity}");
            TempData["Success"] = $"Stock added for {medicine.MedicineName}.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Adjust(int id)
        {
            var inventory = await _context.Inventories.Include(i => i.Medicine).FirstOrDefaultAsync(i => i.InventoryId == id);
            if (inventory == null) return NotFound();
            ViewBag.MedicineName = inventory.Medicine?.MedicineName;
            return View(new InventoryViewModel
            {
                InventoryId = inventory.InventoryId,
                MedicineId = inventory.MedicineId,
                AvailableQuantity = inventory.AvailableQuantity,
                ReorderLevel = inventory.ReorderLevel,
                BatchNumber = inventory.BatchNumber,
                ExpiryDate = inventory.ExpiryDate
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Adjust(int id, InventoryViewModel vm)
        {
            var inventory = await _context.Inventories.Include(i => i.Medicine).FirstOrDefaultAsync(i => i.InventoryId == id);
            if (inventory == null) return NotFound();
            if (!ModelState.IsValid)
            {
                ViewBag.MedicineName = inventory.Medicine?.MedicineName;
                return View(vm);
            }
            inventory.AvailableQuantity = vm.AvailableQuantity;
            inventory.ReorderLevel = vm.ReorderLevel;
            inventory.BatchNumber = vm.BatchNumber;
            inventory.ExpiryDate = vm.ExpiryDate;
            inventory.LastUpdated = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await _audit.LogAsync(_userManager.GetUserId(User), "AdjustStock", "Inventory", id,
                $"{inventory.Medicine?.MedicineName} set to {vm.AvailableQuantity}");
            TempData["Success"] = "Stock updated.";
            return RedirectToAction(nameof(Index));
        }
    }
}
