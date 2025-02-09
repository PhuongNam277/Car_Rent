using System.ComponentModel.DataAnnotations;

namespace Car_Rent.Models
{
    public class ConfirmOtpModel
    {
        [Required(ErrorMessage = "Email là bắt buộc.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Mã OTP là bắt buộc.")]
        public string Otp { get; set; }
    }
}
