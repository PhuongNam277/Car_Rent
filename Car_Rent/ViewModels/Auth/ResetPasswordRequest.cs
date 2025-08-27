using System.ComponentModel.DataAnnotations;

namespace Car_Rent.ViewModels.Auth
{
    public class ResetPasswordRequest
    {
        [Required, MinLength(8)]
        public string NewPassword { get; set; } = string.Empty;

        [Required, Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
