using System.ComponentModel.DataAnnotations;

namespace Car_Rent.Models
{
    public class LoginViewModel
    {
        [Required]
        public required string Username { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public required string Password { get; set; }
    }
}
