using System.Linq;
using System.Threading.Tasks;
using MediVerify.Data;
using MediVerify.Models;
using MediVerify.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediVerify.Controllers
{
    [Authorize(Policy = "AdminPolicy")]
    public class MedicineController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditService _audit;

        public MedicineController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IAuditService audit)
        {
            _context = context;
            _userManager = userManager;
            _audit = audit;
        }

        public async Task<IActionResult> Index(string q, int page = 1, int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 5) pageSize = 10;
            var query = _context.Medicines.AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(m => m.MedicineName.Contains(q) ||
                                         (m.GenericName != null && m.GenericName.Contains(q)) ||
                                         (m.Manufacturer != null && m.Manufacturer.Contains(q)) ||
                                         (m.Category != null && m.Category.Contains(q)));
            }
            var total = await query.CountAsync();
            var medicines = await query
                .OrderBy(m => m.MedicineName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            ViewBag.Query = q;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Total = total;
            ViewBag.TotalPages = (int)System.Math.Ceiling(total / (double)pageSize);
            return View(medicines);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Medicine model)
        {
            if (!ModelState.IsValid) return View(model);
            _context.Medicines.Add(model);
            await _context.SaveChangesAsync();
            await _audit.LogAsync(_userManager.GetUserId(User), "CreateMedicine", "Medicine", model.MedicineId);
            TempData["Success"] = $"Medicine {model.MedicineName} created.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var med = await _context.Medicines.FindAsync(id);
            if (med == null) return NotFound();
            return View(med);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Medicine model)
        {
            if (id != model.MedicineId) return BadRequest();
            if (!ModelState.IsValid) return View(model);
            _context.Update(model);
            await _context.SaveChangesAsync();
            await _audit.LogAsync(_userManager.GetUserId(User), "EditMedicine", "Medicine", id);
            TempData["Success"] = "Medicine updated.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int id)
        {
            var med = await _context.Medicines.FirstOrDefaultAsync(m => m.MedicineId == id);
            if (med == null) return NotFound();
            ViewBag.Inventory = await _context.Inventories
                .Where(i => i.MedicineId == id)
                .OrderBy(i => i.ExpiryDate)
                .ToListAsync();
            return View(med);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var med = await _context.Medicines.FindAsync(id);
            if (med == null) return NotFound();
            med.IsActive = !med.IsActive;
            await _context.SaveChangesAsync();
            await _audit.LogAsync(_userManager.GetUserId(User), med.IsActive ? "ActivateMedicine" : "DeactivateMedicine", "Medicine", id);
            TempData["Success"] = $"{(med.IsActive ? "Activated" : "Deactivated")} {med.MedicineName}.";
            return RedirectToAction(nameof(Index));
        }
    }
}
