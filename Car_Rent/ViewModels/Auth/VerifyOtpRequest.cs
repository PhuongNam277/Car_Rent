using System.ComponentModel.DataAnnotations;

namespace Car_Rent.ViewModels.Auth
{
    public class VerifyOtpRequest
    {

        [Required, MinLength(6), MaxLength(6)]
        public string Otp { get; set; } = string.Empty; // 6-digit OTP
    }
}
