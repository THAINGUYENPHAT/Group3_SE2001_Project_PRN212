using System;
using System.Collections.Generic;

namespace CoffeeRuu.Models;

public partial class OrderVoucher
{
    public int OrderId { get; set; }

    public int VoucherId { get; set; }

    public decimal DiscountApplied { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual Voucher Voucher { get; set; } = null!;
}
