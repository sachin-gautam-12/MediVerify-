using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MediVerify.ViewModels
{
    public class NotificationViewModel
    {
        public int NotificationId { get; set; }
        public string UserId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        [StringLength(2000)]
        public string Message { get; set; }

        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AuditLogViewModel
    {
        public int AuditLogId { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Action { get; set; }
        public string EntityName { get; set; }
        public int? EntityId { get; set; }
        public DateTime Timestamp { get; set; }
        public string IPAddress { get; set; }
        public string Details { get; set; }
    }

    public class ReportFilterViewModel
    {
        [DataType(DataType.Date)]
        public DateTime? FromDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? ToDate { get; set; }

        public string Status { get; set; }
    }

    public class DashboardSummaryViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalPatients { get; set; }
        public int TotalPrescriptions { get; set; }
        public int PendingReviews { get; set; }
        public int ActiveMedicines { get; set; }
        public int LowStockCount { get; set; }
        public IReadOnlyList<DashboardActivityViewModel> RecentActivity { get; set; } = Array.Empty<DashboardActivityViewModel>();
    }

    public class DashboardActivityViewModel
    {
        public string Description { get; set; }
        public DateTime Timestamp { get; set; }
        public string Status { get; set; }
    }
}