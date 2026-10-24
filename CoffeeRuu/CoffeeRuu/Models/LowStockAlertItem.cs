
namespace CoffeeRuu.Models
{
    public class LowStockAlertItem
    {
        public int IngredientId { get; set; }

        public string IngredientName { get; set; } = "";

        public string Unit { get; set; } = "";

        public decimal CurrentStock { get; set; }

        public decimal MinimumStock { get; set; }

        public decimal ShortageQuantity =>
            Math.Max(0, MinimumStock - CurrentStock);

        public string StockStatus =>
            CurrentStock == 0
                ? "Hết hàng"
                : "Tồn kho thấp";
    }
}
