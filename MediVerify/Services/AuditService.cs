using System;
using System.Linq;
using System.Threading.Tasks;
using MediVerify.Data;
using MediVerify.Models;
using Microsoft.AspNetCore.Http;

namespace MediVerify.Services
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAsync(string userId, string action, string entityName, int? entityId = null, string details = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(userId);
            ArgumentException.ThrowIfNullOrWhiteSpace(action);
            ArgumentException.ThrowIfNullOrWhiteSpace(entityName);

            var http = _httpContextAccessor.HttpContext;
            var forwarded = http?.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var ip = !string.IsNullOrWhiteSpace(forwarded)
                ? forwarded.Split(',')[0].Trim()
                : http?.Connection?.RemoteIpAddress?.ToString();

            var log = new AuditLog
            {
                UserId = userId,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                Timestamp = DateTime.UtcNow,
                Details = details,
                IPAddress = string.IsNullOrWhiteSpace(ip) ? "unknown" : ip
            };
            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}
