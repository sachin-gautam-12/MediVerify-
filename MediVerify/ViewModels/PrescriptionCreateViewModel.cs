using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System;

namespace MediVerify.ViewModels
{
    public class PrescriptionItemViewModel
    {
        [Required]
        public int MedicineId { get; set; }

        [Required]
        public decimal Quantity { get; set; }

        [Range(1, 3650)]
        [Display(Name = "Duration (days)")]
        public int Duration { get; set; }

        [StringLength(500)]
        [Display(Name = "Instructions")]
        public string Instructions { get; set; }

        public string Notes { get; set; }
    }

    public class PrescriptionCreateViewModel
    {
        [Required]
        public int PatientId { get; set; }

        [StringLength(50)]
        [Display(Name = "Prescription number")]
        public string PrescriptionNumber { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Prescription date")]
        public DateTime PrescriptionDate { get; set; } = DateTime.Today;

        [DataType(DataType.Date)]
        [Display(Name = "Valid until")]
        public DateTime? ValidUntil { get; set; }

        [StringLength(2000)]
        public string Notes { get; set; }

        [Required]
        public List<PrescriptionItemViewModel> Items { get; set; } = new();
    }

    public class PrescriptionDetailsViewModel
    {
        public int PrescriptionId { get; set; }
        public string PrescriptionNumber { get; set; }
        public string PatientName { get; set; }
        public string DoctorName { get; set; }
        public DateTime PrescriptionDate { get; set; }
        public DateTime? ValidUntil { get; set; }
        public string Status { get; set; }
        public string ClinicalReviewStatus { get; set; }
        public string Notes { get; set; }
        public bool IsVerified { get; set; }
        public List<PrescriptionItemDetailsViewModel> Items { get; set; } = new();
    }

    public class PrescriptionItemDetailsViewModel
    {
        public int PrescriptionItemId { get; set; }
        public int MedicineId { get; set; }
        public string MedicineName { get; set; }
        public decimal Quantity { get; set; }
        public int Duration { get; set; }
        public string PrescribedInstructions { get; set; }
        public string Notes { get; set; }
    }
}
