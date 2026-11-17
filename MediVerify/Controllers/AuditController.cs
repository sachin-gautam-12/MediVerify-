using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediVerify.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediVerify.Controllers
{
    [Authorize(Policy = "AdminPolicy")]
    public class AuditController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuditController(ApplicationDbContext context) => _context = context;

        public async Task<IActionResult> Index(string q, string action, string entity, DateTime? from, DateTime? to)
        {
            var logs = await Filter(q, action, entity, from, to).Take(500).ToListAsync();
            ViewBag.Query = q;
            ViewBag.Action = action;
            ViewBag.Entity = entity;
            ViewBag.From = from?.ToString("yyyy-MM-dd");
            ViewBag.To = to?.ToString("yyyy-MM-dd");
            ViewBag.Actions = await _context.AuditLogs.Select(a => a.Action).Distinct().OrderBy(a => a).ToListAsync();
            ViewBag.Entities = await _context.AuditLogs.Select(a => a.EntityName).Distinct().OrderBy(a => a).ToListAsync();
            return View(logs);
        }

        public async Task<IActionResult> ExportCsv(string q, string action, string entity, DateTime? from, DateTime? to)
        {
            var logs = await Filter(q, action, entity, from, to).ToListAsync();
            var sb = new StringBuilder();
            sb.AppendLine("Timestamp,User,Action,Entity,EntityId,IPAddress,Details");
            foreach (var log in logs)
            {
                sb.AppendLine(string.Join(",",
                    Escape(log.Timestamp.ToString("u")),
                    Escape(log.User?.Email ?? log.UserId),
                    Escape(log.Action),
                    Escape(log.EntityName),
                    Escape(log.EntityId?.ToString() ?? ""),
                    Escape(log.IPAddress),
                    Escape(log.Details)));
            }
            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"mediverify-audit-{DateTime.UtcNow:yyyyMMddHHmm}.csv");
        }

        private IQueryable<Models.AuditLog> Filter(string q, string action, string entity, DateTime? from, DateTime? to)
        {
            var query = _context.AuditLogs.Include(a => a.User).AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(a => (a.User.Email != null && a.User.Email.Contains(q)) ||
                                         (a.User.FullName != null && a.User.FullName.Contains(q)) ||
                                         (a.Details != null && a.Details.Contains(q)));
            }
            if (!string.IsNullOrWhiteSpace(action)) query = query.Where(a => a.Action == action);
            if (!string.IsNullOrWhiteSpace(entity)) query = query.Where(a => a.EntityName == entity);
            if (from.HasValue) query = query.Where(a => a.Timestamp >= from.Value.Date);
            if (to.HasValue) query = query.Where(a => a.Timestamp < to.Value.Date.AddDays(1));
            return query.OrderByDescending(a => a.Timestamp);
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value)) return "\"\"";
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
    }
}
