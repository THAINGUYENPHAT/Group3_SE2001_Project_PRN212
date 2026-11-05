using System.Windows.Controls;
using CoffeeRuu.Services;
using CoffeeRuu.ViewModels;

namespace CoffeeRuu.Views
{
    public partial class ReportView : Page
    {
        public ReportView()
        {
            InitializeComponent();

            var viewModel = new ReportViewModel(
                new ReportService(App.CreateDbContext),
                new ExcelExportService(),
                new ReportDialogService());

            DataContext = viewModel;

            Loaded += async (_, _) => await viewModel.LoadAsync();
        }
    }
}
