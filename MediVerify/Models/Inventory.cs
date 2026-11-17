using System;
using System.ComponentModel.DataAnnotations;

namespace MediVerify.Models
{
    public class Inventory
    {
        [Key]
        public int InventoryId { get; set; }

        [Required]
        public int MedicineId { get; set; }
        public Medicine Medicine { get; set; }

        [Required]
        public decimal AvailableQuantity { get; set; }
        public decimal ReorderLevel { get; set; }
        public string BatchNumber { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
