using System;
using System.Collections.Generic;

namespace CoffeeRuu.Models;

public partial class OrderStatusHistory
{
    public long OrderStatusHistoryId { get; set; }

    public int OrderId { get; set; }

    public int? ChangedBy { get; set; }

    public string? OldStatus { get; set; }

    public string NewStatus { get; set; } = null!;

    public DateTime ChangedAt { get; set; }

    public string? Note { get; set; }

    public virtual Employee? ChangedByNavigation { get; set; }

    public virtual Order Order { get; set; } = null!;
}
