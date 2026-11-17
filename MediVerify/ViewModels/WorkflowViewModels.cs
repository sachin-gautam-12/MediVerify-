using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MediVerify.ViewModels
{
    public class MedicineSafetyViewModel
    {
        [Required]
        [Display(Name = "Medicine")]
        public int MedicineId { get; set; }

        public int? PrescriptionId { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Alert type")]
        public string AlertType { get; set; }

        [Required]
        [StringLength(2000)]
        public string Description { get; set; }

        [Required]
        [RegularExpression("Low|Medium|High|Critical", ErrorMessage = "Select a valid severity.")]
        public string Severity { get; set; }
    }

    public class DispenseViewModel
    {
        [Required]
        public int PrescriptionId { get; set; }

        [StringLength(1000)]
        public string Notes { get; set; }

        public List<DispenseItemViewModel> Items { get; set; } = new();
    }

    public class DispenseItemViewModel
    {
        public int PrescriptionItemId { get; set; }

        public int MedicineId { get; set; }

        public decimal RequestedQuantity { get; set; }

        [Required]
        [Range(typeof(decimal), "0", "1000000")]
        [Display(Name = "Quantity to dispense")]
        public decimal QuantityToDispense { get; set; }
    }

    public class MedicineViewModel
    {
        public int MedicineId { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Medicine name")]
        public string MedicineName { get; set; }

        [StringLength(200)]
        public string GenericName { get; set; }

        [StringLength(200)]
        public string Manufacturer { get; set; }

        [StringLength(100)]
        public string Category { get; set; }

        [StringLength(2000)]
        public string Description { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class InventoryViewModel
    {
        public int InventoryId { get; set; }

        [Required]
        public int MedicineId { get; set; }

        [Required]
        [Range(typeof(decimal), "0", "1000000000")]
        public decimal AvailableQuantity { get; set; }

        [Range(typeof(decimal), "0", "1000000000")]
        public decimal ReorderLevel { get; set; }

        [StringLength(100)]
        public string BatchNumber { get; set; }

        [DataType(DataType.Date)]
        public DateTime? ExpiryDate { get; set; }
    }
}