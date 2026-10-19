using System;
using System.Collections.Generic;

namespace CoffeeRuu.Models;

public partial class OrderItem
{
    public int OrderItemId { get; set; }

    public int OrderId { get; set; }

    public int ProductSizeId { get; set; }

    public string ProductNameSnapshot { get; set; } = null!;

    public string SizeNameSnapshot { get; set; } = null!;

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public decimal LineTotal { get; set; }

    public string PreparationStatus { get; set; } = null!;

    public string? Note { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual ICollection<OrderItemTopping> OrderItemToppings { get; set; } = new List<OrderItemTopping>();

    public virtual ProductSize ProductSize { get; set; } = null!;
}
