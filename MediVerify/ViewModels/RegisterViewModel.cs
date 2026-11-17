using System;
using System.ComponentModel.DataAnnotations;

namespace MediVerify.ViewModels
{
    public class RegisterViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required]
        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime DateOfBirth { get; set; } = DateTime.Today.AddYears(-30);

        [Required]
        [Display(Name = "Gender")]
        public string Gender { get; set; } = "Unspecified";

        [StringLength(300)]
        public string Address { get; set; }

        [StringLength(100)]
        [Display(Name = "Emergency Contact")]
        public string EmergencyContact { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; }
    }
}
