using System;
using System.Collections.Generic;

namespace CoffeeRuu.Models;

public partial class ToppingRecipe
{
    public int ToppingId { get; set; }

    public int IngredientId { get; set; }

    public decimal QuantityUsed { get; set; }

    public virtual Ingredient Ingredient { get; set; } = null!;

    public virtual Topping Topping { get; set; } = null!;
}
