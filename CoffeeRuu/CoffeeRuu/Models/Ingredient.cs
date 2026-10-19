using System;
using System.Collections.Generic;

namespace CoffeeRuu.Models;

public partial class Ingredient
{
    public int IngredientId { get; set; }

    public string IngredientName { get; set; } = null!;

    public string Unit { get; set; } = null!;

    public decimal CurrentStock { get; set; }

    public decimal MinimumStock { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();

    public virtual ICollection<ProductRecipe> ProductRecipes { get; set; } = new List<ProductRecipe>();

    public virtual ICollection<StockReceiptDetail> StockReceiptDetails { get; set; } = new List<StockReceiptDetail>();

    public virtual ICollection<ToppingRecipe> ToppingRecipes { get; set; } = new List<ToppingRecipe>();
}
