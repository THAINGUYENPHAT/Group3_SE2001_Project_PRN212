using CoffeeRuu.ViewModels;
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

namespace CoffeeRuu.Views
{
    /// <summary>
    /// Interaction logic for KdsView.xaml
    /// </summary>
    public partial class KdsView : Page
    {
        private bool _loaded;
        public KdsView()
        {
            InitializeComponent();
        }

        private async void Page_Loaded(
           object sender,
           RoutedEventArgs e)
        {
            if (_loaded)
                return;

            _loaded = true;

            if (DataContext is KdsViewModel vm)
            {
                await vm.InitializeAsync();
            }
        }
    }
}
