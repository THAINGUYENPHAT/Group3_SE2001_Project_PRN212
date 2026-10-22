using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeRuu.Models;
    public class CreateStockReceiptRequest
    {
        public int SupplierId { get; set; }

        public int ReceivedBy { get; set; }

        public string? Note { get; set; }

        public List<StockReceiptItemRequest> Items { get; set; }
            = new();
    }

    public class StockReceiptItemRequest
    {
        public int IngredientId { get; set; }

        public decimal Quantity { get; set; }

        public decimal UnitCost { get; set; }
    }

