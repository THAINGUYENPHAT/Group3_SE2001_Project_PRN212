using System;
using System.Collections.Generic;
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

    public virtual ICollection<ProductSize> ProductSizes { get; set; } = new List<ProductSize>();

    // Giá từng size, lấy từ bảng ProductSizes
    public decimal? PriceS =>
        ProductSizes.FirstOrDefault(
            s => s.SizeName.EndsWith("(S)") && s.IsAvailable)?.Price;

    public decimal? PriceM =>
        ProductSizes.FirstOrDefault(
            s => s.SizeName.EndsWith("(M)") && s.IsAvailable)?.Price;

    public decimal? PriceL =>
        ProductSizes.FirstOrDefault(
            s => s.SizeName.EndsWith("(L)") && s.IsAvailable)?.Price;

}
