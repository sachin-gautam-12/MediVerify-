using System.ComponentModel.DataAnnotations;

namespace MediVerify.ViewModels
{
    public class VerifyPrescriptionViewModel
    {
        [Required]
        public int PrescriptionId { get; set; }

        [Required]
        public bool Approve { get; set; }

        [StringLength(1000)]
        public string Reason { get; set; }
    }
}
