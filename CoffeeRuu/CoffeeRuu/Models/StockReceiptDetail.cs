using System;
using System.Collections.Generic;

namespace CoffeeRuu.Models;

public partial class StockReceiptDetail
{
    public int StockReceiptDetailId { get; set; }

    public int StockReceiptId { get; set; }

    public int IngredientId { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitCost { get; set; }

    public virtual Ingredient Ingredient { get; set; } = null!;

    public virtual StockReceipt StockReceipt { get; set; } = null!;
}
