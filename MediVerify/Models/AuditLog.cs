using System;
using System.ComponentModel.DataAnnotations;

namespace MediVerify.Models
{
    public class AuditLog
    {
        [Key]
        public int AuditLogId { get; set; }

        [Required]
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        [Required]
        public string Action { get; set; }
        public string EntityName { get; set; }
        public int? EntityId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string IPAddress { get; set; }
        public string Details { get; set; }
    }
}
