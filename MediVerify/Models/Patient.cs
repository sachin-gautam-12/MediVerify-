using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MediVerify.Models
{
    public class Patient
    {
        [Key]
        public int PatientId { get; set; }

        [Required]
        public string ApplicationUserId { get; set; }
        public ApplicationUser ApplicationUser { get; set; }

        [Required]
        public DateTime DateOfBirth { get; set; }

        [Required]
        public string Gender { get; set; }

        public string Address { get; set; }
        public string EmergencyContact { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Prescription> Prescriptions { get; set; }
    }
}
