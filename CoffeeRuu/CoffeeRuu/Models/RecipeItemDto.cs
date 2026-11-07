using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CoffeeRuu.Models
{
    public class RecipeItemDto : INotifyPropertyChanged
    {
        public int ProductSizeId { get; set; }
        public int IngredientId { get; set; }
        public string IngredientName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;

        private decimal _quantityUsed;
        public decimal QuantityUsed
        {
            get => _quantityUsed;
            set
            {
                if (_quantityUsed != value)
                {
                    _quantityUsed = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}