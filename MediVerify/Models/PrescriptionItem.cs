using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MediVerify.Models
{
    public class PrescriptionItem
    {
        [Key]
        public int PrescriptionItemId { get; set; }

        [Required]
        public int PrescriptionId { get; set; }
        public Prescription Prescription { get; set; }

        [Required]
        public int MedicineId { get; set; }
        public Medicine Medicine { get; set; }

        [Required]
        public decimal Quantity { get; set; }
        public string PrescribedInstructions { get; set; }
        public int Duration { get; set; } // days
        public string Notes { get; set; }
    }
}
