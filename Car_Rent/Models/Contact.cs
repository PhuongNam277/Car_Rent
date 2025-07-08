using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Car_Rent.Models;

public partial class Contact
{
    public int ContactId { get; set; }

    [Required(ErrorMessage = "Tên không được để trống")]
    public string Name { get; set; } = null!;

    [Required(ErrorMessage = "Email không được để trống")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [RegularExpression(@"^[a-zA-Z0-9._%+-]+@gmail\.com$", ErrorMessage = "Chỉ chấp nhận email @gmail.com")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Số điện thoại không được để trống")]
    [RegularExpression(@"^\d{10}$", ErrorMessage = "Số điện thoại phải đúng 10 chữ số")]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Tin nhắn không được để trống")]
    public string Message { get; set; } = null!;

    public DateTime? SubmittedDate { get; set; }

    public string? Status { get; set; }

    [Required(ErrorMessage = "Project không được để trống")]
    public string? Project { get; set; }

    [Required(ErrorMessage = "Subject không được để trống")]
    public string? Subject { get; set; }

}
