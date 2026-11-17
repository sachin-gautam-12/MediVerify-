using System;
using System.ComponentModel.DataAnnotations;

namespace MediVerify.Models
{
    public class SafetyAlert
    {
        [Key]
        public int SafetyAlertId { get; set; }

        public int? PrescriptionId { get; set; }
        public Prescription Prescription { get; set; }

        [Required]
        public int MedicineId { get; set; }
        public Medicine Medicine { get; set; }

        [Required]
        public string AlertType { get; set; }
        public string Description { get; set; }
        public string Severity { get; set; }
        public string ReviewStatus { get; set; } = "Pending";
        public string ReviewedBy { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
