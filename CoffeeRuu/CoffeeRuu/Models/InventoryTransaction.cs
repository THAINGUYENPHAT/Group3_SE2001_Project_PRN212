using System;
using System.Collections.Generic;

namespace CoffeeRuu.Models;

public partial class InventoryTransaction
{
    public long InventoryTransactionId { get; set; }

    public int IngredientId { get; set; }

    public int? EmployeeId { get; set; }

    public string TransactionType { get; set; } = null!;

    public decimal QuantityChange { get; set; }

    public string? ReferenceType { get; set; }

    public int? ReferenceId { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? Note { get; set; }

    public virtual Employee? Employee { get; set; }

    public virtual Ingredient Ingredient { get; set; } = null!;
}
