
using System;
using System.Windows.Controls;
using CoffeeRuu.ViewModels;

namespace CoffeeRuu.Views
{
    public partial class TableView : UserControl
    {
        public TableView(TableViewModel viewModel)
        {
            InitializeComponent();

            DataContext = viewModel
                ?? throw new ArgumentNullException(nameof(viewModel));
        }
    }
}
