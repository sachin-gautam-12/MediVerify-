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
    [Authorize(Roles = "Administrator,Pharmacist")]
    public class SafetyAlertController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ISafetyService _safety;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditService _audit;

        public SafetyAlertController(ApplicationDbContext context, ISafetyService safety,
            UserManager<ApplicationUser> userManager, IAuditService audit)
        {
            _context = context;
            _safety = safety;
            _userManager = userManager;
            _audit = audit;
        }

        public async Task<IActionResult> Index(string status, string severity)
        {
            var query = _context.SafetyAlerts
                .Include(a => a.Medicine)
                .Include(a => a.Prescription)
                .AsQueryable();
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(a => a.ReviewStatus == status);
            if (!string.IsNullOrWhiteSpace(severity)) query = query.Where(a => a.Severity == severity);
            var alerts = await query.OrderByDescending(a => a.CreatedAt).Take(300).ToListAsync();
            ViewBag.Status = status;
            ViewBag.Severity = severity;
            return View(alerts);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Acknowledge(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (await _safety.AcknowledgeAsync(id, userId))
            {
                await _audit.LogAsync(userId, "AcknowledgeSafetyAlert", "SafetyAlert", id);
                TempData["Success"] = "Alert acknowledged.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Resolve(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (await _safety.ResolveAsync(id, userId))
            {
                await _audit.LogAsync(userId, "ResolveSafetyAlert", "SafetyAlert", id);
                TempData["Success"] = "Alert resolved.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
