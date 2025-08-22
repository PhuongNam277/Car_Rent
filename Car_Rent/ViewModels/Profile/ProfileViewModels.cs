using System.ComponentModel.DataAnnotations;
using Org.BouncyCastle.Bcpg.OpenPgp;

namespace Car_Rent.ViewModels.Profile
{
    public class ProfileViewModel
    {
        public int UserId { get; set; }
        public string Username { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string? PhoneNumber { get; set; }
        public string? RoleName { get; set; } = "";
        public DateTime? CreatedDate { get; set; }
        public string? AvatarUrl { get; set; } // Calculate from file, not save to db
    }

    public class ProfileUpdateRequest
    {
        [Required, StringLength(100, MinimumLength = 2)]
        public string FullName { get; set; } = "";
        [RegularExpression(@"^\+?[0-9]{8,15}$", ErrorMessage = "Phone number is invalid.")]
        public string? PhoneNumber { get; set; }

        // Optional avatar
        public IFormFile? Avatar { get; set; }
    }

    public class ChangePasswordRequest
    {
        [Required] public string CurrentPassword { get; set; } = "";

        [Required, MinLength(8, ErrorMessage = "New password must be at least 8 characters long.")]
        public string NewPassword { get; set; } = "";

        [Required, Compare(nameof(NewPassword), ErrorMessage = "Confirm password does not match.")]
        public string ConfirmNewPassword { get; set; } = "";
    }

    public class RequestEmailChangeViewModel
    {
        [Required(ErrorMessage = "Please enter an email address.")]
        [RegularExpression(@"^[^@\s]+@gmail\.com$", ErrorMessage = "Only @gmail.com emails are allowed.")]
        public string NewEmail { get; set; } = "";
    }

    public class ConfirmEmailChangeViewModel
    {
        [Required(ErrorMessage = "Please enter an email address.")]
        [RegularExpression(@"^[^@\s]+@gmail\.com$", ErrorMessage = "Only @gmail.com emails are allowed.")]
        public string NewEmail { get; set; } = "";


        [Required, StringLength(6, MinimumLength =6, ErrorMessage = "OTP must be 6 characters long.")]
        public string Otp { get; set; } = "";
    }

    public class RequestResetPassword
    {
        [Required]
        [MinLength(8, ErrorMessage = "New password must be at least 8 characters long.")]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; } = "";

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
        public string ConfirmNewPassword { get; set; } = "";
    }
}
