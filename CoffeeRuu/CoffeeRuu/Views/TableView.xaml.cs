using System.Windows.Controls;
using CoffeeRuu.Data;
using CoffeeRuu.Repositories;
using CoffeeRuu.Services;
using CoffeeRuu.ViewModels;

namespace CoffeeRuu.Views
{
    public partial class TableView : UserControl
    {
        public TableView()
        {
            InitializeComponent();
            // Khởi tạo các Dependencies
            var repo = new TableRepository(() => App.CreateDbContext());
            var service = new TableService(repo);
            DataContext = new TableViewModel(service);
        }
    }
}