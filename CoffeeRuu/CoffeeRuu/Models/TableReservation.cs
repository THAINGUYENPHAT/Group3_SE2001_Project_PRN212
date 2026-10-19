using System;
using System.Collections.Generic;

namespace CoffeeRuu.Models;

public partial class TableReservation
{
    public int ReservationId { get; set; }

    public int TableId { get; set; }

    public int CreatedBy { get; set; }

    public string CustomerName { get; set; } = null!;

    public string? CustomerPhone { get; set; }

    public int GuestCount { get; set; }

    public DateTime ReservationAt { get; set; }

    public string ReservationStatus { get; set; } = null!;

    public string? Note { get; set; }

    public virtual Employee CreatedByNavigation { get; set; } = null!;

    public virtual CafeTable Table { get; set; } = null!;
}
