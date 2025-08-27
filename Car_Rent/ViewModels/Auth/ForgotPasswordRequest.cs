using System.ComponentModel.DataAnnotations;

namespace Car_Rent.ViewModels.Auth
{
    public class ForgotPasswordRequest
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
