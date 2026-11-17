using System;
using System.ComponentModel.DataAnnotations;

namespace MediVerify.ViewModels
{
    public class PatientProfileViewModel
    {
        public string Email { get; set; }

        [Required]
        [Display(Name = "Full name")]
        public string FullName { get; set; }

        [Phone]
        [Display(Name = "Phone number")]
        public string PhoneNumber { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Date of birth")]
        public DateTime DateOfBirth { get; set; }

        [Required]
        public string Gender { get; set; }

        [StringLength(300)]
        public string Address { get; set; }

        [StringLength(100)]
        [Display(Name = "Emergency contact")]
        public string EmergencyContact { get; set; }
    }
}
