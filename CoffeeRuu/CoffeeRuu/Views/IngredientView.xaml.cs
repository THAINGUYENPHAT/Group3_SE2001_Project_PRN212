using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CoffeeRuu.Repositories;
using CoffeeRuu.Services;
using CoffeeRuu.ViewModels;

namespace CoffeeRuu.Views
{
    public partial class IngredientView : Page
    {
        public IngredientView()
        {
            InitializeComponent();

            var repository = new IngredientRepository(
                App.CreateDbContext
            );

            var service = new InventoryService(repository);

            DataContext = new IngredientViewModel(service);
        }
    }
}

