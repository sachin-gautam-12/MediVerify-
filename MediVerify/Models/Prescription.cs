using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MediVerify.Models
{
    public class Prescription
    {
        [Key]
        public int PrescriptionId { get; set; }

        [Required]
        public int PatientId { get; set; }
        public Patient Patient { get; set; }

        [Required]
        public string DoctorId { get; set; }
        public ApplicationUser Doctor { get; set; }

        [Required]
        public string PrescriptionNumber { get; set; }
        public DateTime PrescriptionDate { get; set; } = DateTime.UtcNow;
        public DateTime? ValidUntil { get; set; }
        public string Status { get; set; } = "Draft"; // Draft, Submitted, Verified, Dispensed
        public string ClinicalReviewStatus { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsVerified { get; set; } = false;

        public string RejectionReason { get; set; }
        public string VerifiedBy { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public DateTime? DispensedAt { get; set; }

        public ICollection<PrescriptionItem> Items { get; set; }
    }
}
