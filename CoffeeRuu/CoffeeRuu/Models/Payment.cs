using System;
using System.Collections.Generic;

namespace CoffeeRuu.Models;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int OrderId { get; set; }

    public int ProcessedBy { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public string PaymentStatus { get; set; } = null!;

    public decimal Amount { get; set; }

    public string? TransactionRef { get; set; }

    public DateTime? PaidAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? Note { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual Employee ProcessedByNavigation { get; set; } = null!;
}
