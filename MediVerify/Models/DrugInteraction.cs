using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MediVerify.Models
{
    public class DrugInteraction
    {
        [Key]
        public int DrugInteractionId { get; set; }

        [Required]
        public int MedicineAId { get; set; }
        [ForeignKey(nameof(MedicineAId))]
        public Medicine MedicineA { get; set; }

        [Required]
        public int MedicineBId { get; set; }
        [ForeignKey(nameof(MedicineBId))]
        public Medicine MedicineB { get; set; }

        [Required]
        [StringLength(20)]
        public string Severity { get; set; } = "Moderate"; // Low, Moderate, High

        [Required]
        [StringLength(1000)]
        public string Description { get; set; }
    }
}
