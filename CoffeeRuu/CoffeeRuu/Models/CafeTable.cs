using System;
using System.Collections.Generic;

namespace CoffeeRuu.Models;

public partial class CafeTable
{
    public int TableId { get; set; }

    public string TableName { get; set; } = null!;

    public int Capacity { get; set; }

    public string TableStatus { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<TableReservation> TableReservations { get; set; } = new List<TableReservation>();
}
