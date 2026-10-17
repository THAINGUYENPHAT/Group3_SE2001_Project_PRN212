using System;
using System.Collections.Generic;

namespace CoffeeRuu.Models;

public partial class AuditLog
{
    public long AuditLogId { get; set; }

    public int? EmployeeId { get; set; }

    public string Action { get; set; } = null!;

    public string? EntityName { get; set; }

    public string? EntityId { get; set; }

    public string? Details { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Employee? Employee { get; set; }
}
