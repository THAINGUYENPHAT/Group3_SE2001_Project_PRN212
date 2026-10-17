using System;
using System.Collections.Generic;

namespace CoffeeRuu.Models;

public partial class Voucher
{
    public int VoucherId { get; set; }

    public string VoucherCode { get; set; } = null!;

    public string? Description { get; set; }

    public string DiscountType { get; set; } = null!;

    public decimal DiscountValue { get; set; }

    public decimal MinimumOrder { get; set; }

    public decimal? MaximumDiscount { get; set; }

    public DateTime StartAt { get; set; }

    public DateTime EndAt { get; set; }

    public int? UsageLimit { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<OrderVoucher> OrderVouchers { get; set; } = new List<OrderVoucher>();
}
