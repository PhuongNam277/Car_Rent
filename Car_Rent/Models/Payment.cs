﻿using System;
using System.Collections.Generic;

namespace Car_Rent.Models;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int ReservationId { get; set; }

    public DateTime? PaymentDate { get; set; }

    public decimal Amount { get; set; }

    public string? PaymentMethod { get; set; }

    public string? Status { get; set; }

    public virtual Reservation Reservation { get; set; } = null!;
}
