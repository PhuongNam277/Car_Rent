using System;
using System.Collections.Generic;

namespace Car_Rent.Models;

public partial class Contact
{
    public int ContactId { get; set; }

    public string Name { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public string Message { get; set; } = null!;

    public DateTime? SubmittedDate { get; set; }

    public string? Status { get; set; }
}
