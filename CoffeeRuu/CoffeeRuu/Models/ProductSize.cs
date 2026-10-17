using System;
using System.Collections.Generic;

namespace CoffeeRuu.Models;

public partial class ProductSize
{
    public int ProductSizeId { get; set; }

    public int ProductId { get; set; }

    public string SizeName { get; set; } = null!;

    public decimal Price { get; set; }

    public bool IsAvailable { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual Product Product { get; set; } = null!;

    public virtual ICollection<ProductRecipe> ProductRecipes { get; set; } = new List<ProductRecipe>();
}
