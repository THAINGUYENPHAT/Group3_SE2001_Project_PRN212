using System.Windows;
using Microsoft.Win32;

namespace CoffeeRuu.Services
{
    public class ReportDialogService : IReportDialogService
    {
        public void ShowInfo(string title, string message) =>
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);

        public void ShowWarning(string title, string message) =>
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);

        public void ShowError(string title, string message) =>
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);

        public string? PickExcelSavePath(string defaultFileName)
        {
            var dialog = new SaveFileDialog
            {
                Title = "Xuất báo cáo CoffeeRuu",
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                FileName = defaultFileName,
                DefaultExt = ".xlsx",
                AddExtension = true
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }
    }
}
