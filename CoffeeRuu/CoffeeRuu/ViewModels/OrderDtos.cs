using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeRuu.ViewModels
{
    public class MenuChoice
    {
        public int ProductSizeId { get; set; }
        public string ProductName { get; set; } = "";
        public string SizeName { get; set; } = "";
        public decimal Price { get; set; }

        public string Display =>
            $"{ProductName} - {SizeName} ({Price:N0} đ)";
    }

    public class TableChoice
    {
        public int TableId { get; set; }
        public string TableName { get; set; } = "";
    }

    public class CartLine : INotifyPropertyChanged
    {
        private int _quantity;

        public int ProductSizeId { get; set; }

        public string ProductName { get; set; } = "";

        public string SizeName { get; set; } = "";

        public string DisplayName =>
            string.IsNullOrEmpty(SizeName)
                ? ProductName
                : $"{ProductName} ({SizeName})";

        public decimal UnitPrice { get; set; }

        public int Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity != value)
                {
                    _quantity = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(LineTotal));
                }
            }
        }

        public string Note { get; set; } = "";

        public decimal LineTotal => UnitPrice * Quantity;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
    }

    public class OrderListRow
    {
        public int OrderId { get; set; }
        public string OrderCode { get; set; } = "";
        public string TableName { get; set; } = "";
        public string OrderType { get; set; } = "";
        public string OrderStatus { get; set; } = "";
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class KdsItemRow
    {
        public int OrderItemId { get; set; }
        public int OrderId { get; set; }

        public string OrderCode { get; set; } = "";
        public string TableName { get; set; } = "";

        public string ProductName { get; set; } = "";
        public string SizeName { get; set; } = "";

        public int Quantity { get; set; }
        public string Note { get; set; } = "";

        public string PreparationStatus { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }

    public sealed class EmployeeChoice
    {
        public int EmployeeId { get; set; }
        public string FullName { get; set; } = "";
        public string RoleName { get; set; } = "";

        public string DisplayName =>
            $"{EmployeeId} - {FullName} ({RoleName})";
    }

}
