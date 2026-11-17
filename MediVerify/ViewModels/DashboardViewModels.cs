using System.Collections.Generic;
using MediVerify.Models;

namespace MediVerify.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalPatients { get; set; }
        public int TotalPrescriptions { get; set; }
        public int PendingReviews { get; set; }
        public int ActiveMedicines { get; set; }
        public int LowStockCount { get; set; }
    }

    public class DoctorDashboardViewModel
    {
        public List<Patient> MyPatients { get; set; }
        public List<Prescription> MyPrescriptions { get; set; }
    }

    public class PharmacistDashboardViewModel
    {
        public List<Prescription> PendingPrescriptions { get; set; }
        public List<Medicine> LowStockMedicines { get; set; }
    }

    public class PatientDashboardViewModel
    {
        public List<Prescription> Prescriptions { get; set; }
    }
}
