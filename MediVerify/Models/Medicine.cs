using System;
using System.ComponentModel.DataAnnotations;

namespace MediVerify.Models
{
    public class Medicine
    {
        [Key]
        public int MedicineId { get; set; }

        [Required]
        public string MedicineName { get; set; }
        public string GenericName { get; set; }
        public string Manufacturer { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
