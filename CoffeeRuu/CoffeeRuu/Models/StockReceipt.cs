using System;
using System.Collections.Generic;

namespace CoffeeRuu.Models;

public partial class StockReceipt
{
    public int StockReceiptId { get; set; }

    public int SupplierId { get; set; }

    public int ReceivedBy { get; set; }

    public DateTime ReceiptDate { get; set; }

    public string? Note { get; set; }

    public decimal TotalCost { get; set; }

    public virtual Employee ReceivedByNavigation { get; set; } = null!;

    public virtual ICollection<StockReceiptDetail> StockReceiptDetails { get; set; } = new List<StockReceiptDetail>();

    public virtual Supplier Supplier { get; set; } = null!;
}
