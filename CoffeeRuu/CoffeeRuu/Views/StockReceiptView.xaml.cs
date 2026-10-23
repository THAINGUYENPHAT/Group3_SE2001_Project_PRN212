
using CoffeeRuu.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;

namespace CoffeeRuu.Views;

public partial class StockReceiptView : Page
{
    private readonly StockReceiptViewModel _viewModel;
    private bool _isLoaded;

    public StockReceiptView(
        StockReceiptViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel
            ?? throw new ArgumentNullException(nameof(viewModel));

        DataContext = _viewModel;
    }

    private async void Page_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        if (_isLoaded)
            return;

        _isLoaded = true;

        await _viewModel.LoadAsync();
    }
}
