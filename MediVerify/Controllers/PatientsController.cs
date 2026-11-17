using System.Linq;
using System.Threading.Tasks;
using MediVerify.Data;
using MediVerify.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediVerify.Controllers
{
    [Authorize(Policy = "DoctorPolicy")]
    public class PatientsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PatientsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var doctorId = _userManager.GetUserId(User);
            var patientIds = _context.Prescriptions
                .Where(p => p.DoctorId == doctorId)
                .Select(p => p.PatientId)
                .Distinct();

            var patients = await _context.Patients
                .Include(p => p.ApplicationUser)
                .Where(p => patientIds.Contains(p.PatientId))
                .OrderBy(p => p.ApplicationUser.FullName)
                .ToListAsync();

            ViewBag.PrescriptionCounts = await _context.Prescriptions
                .Where(p => p.DoctorId == doctorId)
                .GroupBy(p => p.PatientId)
                .Select(g => new { PatientId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.PatientId, x => x.Count);

            return View(patients);
        }

        public async Task<IActionResult> Details(int id)
        {
            var doctorId = _userManager.GetUserId(User);
            var patient = await _context.Patients.Include(p => p.ApplicationUser).FirstOrDefaultAsync(p => p.PatientId == id);
            if (patient == null) return NotFound();
            var prescriptions = await _context.Prescriptions
                .Where(p => p.PatientId == id && p.DoctorId == doctorId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
            if (!prescriptions.Any()) return Forbid();
            ViewBag.Prescriptions = prescriptions;
            return View(patient);
        }
    }
}
