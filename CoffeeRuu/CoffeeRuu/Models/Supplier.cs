using System;
using System.Collections.Generic;

namespace CoffeeRuu.Models;

public partial class Supplier
{
    public int SupplierId { get; set; }

    public string SupplierName { get; set; } = null!;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<StockReceipt> StockReceipts { get; set; } = new List<StockReceipt>();
}
