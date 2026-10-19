using System;
using System.Collections.Generic;

namespace CoffeeRuu.Models;

public partial class ProductRecipe
{
    public int ProductSizeId { get; set; }

    public int IngredientId { get; set; }

    public decimal QuantityUsed { get; set; }

    public virtual Ingredient Ingredient { get; set; } = null!;

    public virtual ProductSize ProductSize { get; set; } = null!;
}
