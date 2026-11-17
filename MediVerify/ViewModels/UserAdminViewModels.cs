using System;
using System.ComponentModel.DataAnnotations;
using MediVerify.Models;

namespace MediVerify.ViewModels
{
    public class UserCreateViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [Display(Name = "Full name")]
        public string FullName { get; set; }

        [Phone]
        [Display(Name = "Phone number")]
        public string PhoneNumber { get; set; }

        [Required]
        [Display(Name = "Role")]
        public string Role { get; set; } = "Patient";

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "Welcome#123";

        [DataType(DataType.Date)]
        [Display(Name = "Date of birth (patients)")]
        public DateTime? DateOfBirth { get; set; }

        [Display(Name = "Gender (patients)")]
        public string Gender { get; set; } = "Unspecified";
    }

    public class ChangeRoleViewModel
    {
        [Required]
        public string UserId { get; set; }

        [Required]
        public string Role { get; set; }
    }

    public class UserRowViewModel
    {
        public ApplicationUser User { get; set; }
        public string Role { get; set; }
    }
}
