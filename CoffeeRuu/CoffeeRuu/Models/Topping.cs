using System;
using System.Collections.Generic;

namespace CoffeeRuu.Models;

public partial class Topping
{
    public int ToppingId { get; set; }

    public string ToppingName { get; set; } = null!;

    public decimal ExtraPrice { get; set; }

    public bool IsAvailable { get; set; }

    public virtual ICollection<OrderItemTopping> OrderItemToppings { get; set; } = new List<OrderItemTopping>();

    public virtual ICollection<ToppingRecipe> ToppingRecipes { get; set; } = new List<ToppingRecipe>();
}
