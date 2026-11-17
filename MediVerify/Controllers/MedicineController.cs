using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediVerify.Data;
using MediVerify.Models;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace MediVerify.Controllers
{
    [Authorize(Policy = "AdminPolicy")]
    public class MedicineController : Controller
    {
        private readonly ApplicationDbContext _context;
        public MedicineController(ApplicationDbContext context) => _context = context;

        public async Task<IActionResult> Index()
        {
            var medicines = await _context.Medicines.ToListAsync();
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
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int id)
        {
            var med = await _context.Medicines.FindAsync(id);
            if (med == null) return NotFound();
            return View(med);
        }
    }
}
