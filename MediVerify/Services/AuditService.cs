using System;
using System.Threading.Tasks;
using MediVerify.Data;
using MediVerify.Models;
using Microsoft.EntityFrameworkCore;

namespace MediVerify.Services
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;
        public AuditService(ApplicationDbContext context) => _context = context;

        public async Task LogAsync(string userId, string action, string entityName, int? entityId = null, string details = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(userId);
            ArgumentException.ThrowIfNullOrWhiteSpace(action);
            ArgumentException.ThrowIfNullOrWhiteSpace(entityName);

            var log = new AuditLog
            {
                UserId = userId,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                Timestamp = DateTime.UtcNow,
                Details = details,
                IPAddress = "0.0.0.0" // placeholder – could capture from HttpContext in real implementation
            };
            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}
