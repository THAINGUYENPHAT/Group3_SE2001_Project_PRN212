using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CoffeeRuu.Models;
using CoffeeRuu.Services;

namespace CoffeeRuu.ViewModels
{
    public class ProductRecipeViewModel : INotifyPropertyChanged
    {
        private readonly InventoryService _inventoryService;

        public ObservableCollection<ProductSize> ProductSizes { get; } = new();
        public ObservableCollection<Ingredient> AvailableIngredients { get; } = new();
        public ObservableCollection<RecipeItemDto> CurrentRecipeItems { get; } = new();

        private ProductSize? _selectedProductSize;
        public ProductSize? SelectedProductSize
        {
            get => _selectedProductSize;
            set
            {
                if (_selectedProductSize == value) return;
                _selectedProductSize = value;
                OnPropertyChanged();
                _ = LoadRecipeByProductSizeAsync();
            }
        }

        private Ingredient? _selectedIngredientToAdd;
        public Ingredient? SelectedIngredientToAdd
        {
            get => _selectedIngredientToAdd;
            set
            {
                _selectedIngredientToAdd = value;
                OnPropertyChanged();
            }
        }

        private string _quantityUsedInput = "0";
        public string QuantityUsedInput
        {
            get => _quantityUsedInput;
            set
            {
                _quantityUsedInput = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand AddIngredientCommand { get; }
        public RelayCommand SaveRecipeCommand { get; }

        public ProductRecipeViewModel(InventoryService inventoryService)
        {
            _inventoryService = inventoryService;
            AddIngredientCommand = new RelayCommand(AddIngredientToRecipe);
            SaveRecipeCommand = new RelayCommand(async () => await SaveRecipeAsync());
        }

        private async Task LoadRecipeByProductSizeAsync()
        {
            CurrentRecipeItems.Clear();
            if (SelectedProductSize == null) return;

            var recipes = await _inventoryService.GetRecipeByProductSizeIdAsync(SelectedProductSize.ProductSizeId);
            foreach (var item in recipes)
            {
                CurrentRecipeItems.Add(item);
            }
        }

        private void AddIngredientToRecipe()
        {
            if (SelectedIngredientToAdd == null || SelectedProductSize == null) return;

            if (!decimal.TryParse(QuantityUsedInput, NumberStyles.Number, CultureInfo.CurrentCulture, out decimal quantityUsed) || quantityUsed <= 0)
                return;

            var existing = CurrentRecipeItems.FirstOrDefault(x => x.IngredientId == SelectedIngredientToAdd.IngredientId);
            if (existing != null)
            {
                existing.QuantityUsed += quantityUsed;
            }
            else
            {
                CurrentRecipeItems.Add(new RecipeItemDto
                {
                    ProductSizeId = SelectedProductSize.ProductSizeId,
                    IngredientId = SelectedIngredientToAdd.IngredientId,
                    IngredientName = SelectedIngredientToAdd.IngredientName,
                    Unit = SelectedIngredientToAdd.Unit,
                    QuantityUsed = quantityUsed
                });
            }

            QuantityUsedInput = "0";
        }

        private async Task SaveRecipeAsync()
        {
            if (SelectedProductSize == null) return;

            var recipesToSave = CurrentRecipeItems.Select(x => new ProductRecipe
            {
                ProductSizeId = SelectedProductSize.ProductSizeId,
                IngredientId = x.IngredientId,
                QuantityUsed = x.QuantityUsed
            }).ToList();

            await _inventoryService.SaveProductRecipeAsync(SelectedProductSize.ProductSizeId, recipesToSave);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}