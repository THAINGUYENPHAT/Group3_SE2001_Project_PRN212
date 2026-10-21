
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace CoffeeRuu.Models;

public partial class Product
{
    public int ProductId { get; set; }

    public int CategoryId { get; set; }

    public string ProductName { get; set; } = null!;

    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsAvailable { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual ICollection<ProductSize> ProductSizes { get; set; }
        = new List<ProductSize>();

    // Size đang được chọn trên giao diện
    [NotMapped]
    public string SelectedSizeName { get; set; } = "Tất cả";

    // Hiển thị giá theo size được chọn
    [NotMapped]
    public string PriceDisplay
    {
        get
        {
            var sizes = ProductSizes?
                .Where(s => s.IsAvailable)
                .ToList();

            if (sizes == null || sizes.Count == 0)
                return "Chưa có giá";

            // Khi chọn một size cụ thể
            if (!string.Equals(
                    SelectedSizeName,
                    "Tất cả",
                    StringComparison.OrdinalIgnoreCase))
            {
                var selectedSize = sizes.FirstOrDefault(s =>
                    string.Equals(
                        s.SizeName,
                        SelectedSizeName,
                        StringComparison.OrdinalIgnoreCase));

                return selectedSize == null
                    ? "Chưa có giá"
                    : selectedSize.Price.ToString("N0");
            }

            // Khi xem tất cả size
            return string.Join(
                " | ",
                sizes.OrderBy(s => s.SizeName)
                     .Select(s =>
                         s.SizeName + ": " + s.Price.ToString("N0")));
        }
    }
}
