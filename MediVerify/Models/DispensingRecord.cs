using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MediVerify.Models
{
    public class DispensingRecord
    {
        [Key]
        public int DispensingRecordId { get; set; }

        [Required]
        public int PrescriptionId { get; set; }
        public Prescription Prescription { get; set; }

        [Required]
        public string PharmacistId { get; set; }
        public ApplicationUser Pharmacist { get; set; }

        public DateTime DispensedAt { get; set; } = DateTime.UtcNow;

        // Simple representation of dispensed items – quantity per medicine
        public ICollection<DispensedItem> Items { get; set; }
    }

    public class DispensedItem
    {
        [Key]
        public int DispensedItemId { get; set; }

        [Required]
        public int DispensingRecordId { get; set; }
        public DispensingRecord DispensingRecord { get; set; }

        [Required]
        public int MedicineId { get; set; }
        public Medicine Medicine { get; set; }

        [Required]
        public decimal Quantity { get; set; }
    }
}
