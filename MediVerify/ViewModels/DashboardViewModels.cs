using System.Collections.Generic;
using MediVerify.Models;

namespace MediVerify.ViewModels
{
    public class ChartSeriesViewModel
    {
        public List<string> Labels { get; set; } = new();
        public List<int> Values { get; set; } = new();
    }

    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalPatients { get; set; }
        public int TotalPrescriptions { get; set; }
        public int PendingReviews { get; set; }
        public int ActiveMedicines { get; set; }
        public int LowStockCount { get; set; }
        public int DispensedToday { get; set; }
        public ChartSeriesViewModel PrescriptionsPerDay { get; set; } = new();
        public ChartSeriesViewModel StatusBreakdown { get; set; } = new();
        public ChartSeriesViewModel TopMedicines { get; set; } = new();
        public List<AuditLog> RecentActivity { get; set; } = new();
        public List<Inventory> LowStockItems { get; set; } = new();
    }

    public class DoctorDashboardViewModel
    {
        public List<Patient> MyPatients { get; set; } = new();
        public List<Prescription> MyPrescriptions { get; set; } = new();
        public int DraftCount { get; set; }
        public int SubmittedCount { get; set; }
        public int DispensedCount { get; set; }
        public ChartSeriesViewModel PrescriptionsPerDay { get; set; } = new();
        public ChartSeriesViewModel StatusBreakdown { get; set; } = new();
    }

    public class PharmacistDashboardViewModel
    {
        public List<Prescription> PendingPrescriptions { get; set; } = new();
        public List<Medicine> LowStockMedicines { get; set; } = new();
        public int VerifiedToday { get; set; }
        public int DispensedToday { get; set; }
        public int OpenAlerts { get; set; }
        public ChartSeriesViewModel StatusBreakdown { get; set; } = new();
        public ChartSeriesViewModel StockByCategory { get; set; } = new();
        public List<Inventory> LowStockItems { get; set; } = new();
    }

    public class PatientDashboardViewModel
    {
        public List<Prescription> Prescriptions { get; set; } = new();
        public int ActiveCount { get; set; }
        public int DispensedCount { get; set; }
        public int PendingCount { get; set; }
        public ChartSeriesViewModel StatusBreakdown { get; set; } = new();
        public List<Notification> Notifications { get; set; } = new();
    }
}
