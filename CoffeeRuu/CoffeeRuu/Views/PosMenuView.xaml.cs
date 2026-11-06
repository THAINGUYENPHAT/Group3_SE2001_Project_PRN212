using CoffeeRuu.Repositories;
using CoffeeRuu.Services;
using CoffeeRuu.ViewModels;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CoffeeRuu.Views
{
    public partial class PosMenuView : UserControl
    {
        private readonly PosMenuViewModel _viewModel;

        public PosMenuView()
        {
            InitializeComponent();

            var menuService = new MenuService(
                new MenuRepository(App.CreateDbContext()));

            var orderService = new OrderService(
                new OrderRepository(App.CreateDbContext));

            IPrintService printService = new PrintService();

            _viewModel = new PosMenuViewModel(
                menuService,
                orderService,
                printService);

            // Các hộp thoại là phần giao tiếp giữa View và ViewModel.
            _viewModel.ShowItemNoteDialog = ShowItemNoteDialog;
            _viewModel.ShowQrPaymentDialog = ShowQrPaymentDialog;
            _viewModel.ShowMessage = ShowMessage;
            _viewModel.NavigateToKds = NavigateToKds;

            DataContext = _viewModel;

            Loaded += PosMenuView_Loaded;
        }

        private void PosMenuView_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            _viewModel.LoadDataCommand.Execute(null);
        }

        // Hộp thoại ghi chú món.
        private string? ShowItemNoteDialog(
            string productName,
            string sizeName)
        {
            var dialog = new Window
            {
                Title = "Ghi chú món",
                Width = 420,
                Height = 280,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this),
                ResizeMode = ResizeMode.NoResize,
                Background = Brushes.White
            };

            var panel = new StackPanel
            {
                Margin = new Thickness(20)
            };

            panel.Children.Add(new TextBlock
            {
                Text = $"{productName} - {sizeName}",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 15)
            });

            panel.Children.Add(new TextBlock
            {
                Text = "Ghi chú (có thể bỏ trống):",
                Margin = new Thickness(0, 0, 0, 8)
            });

            var noteBox = new TextBox
            {
                Height = 80,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                MaxLength = 300,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            panel.Children.Add(noteBox);

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 15, 0, 0)
            };

            var cancelButton = new Button
            {
                Content = "Hủy",
                Width = 90,
                Padding = new Thickness(10, 6, 10, 6),
                Margin = new Thickness(0, 0, 10, 0)
            };

            var addButton = new Button
            {
                Content = "Thêm món",
                Width = 100,
                Padding = new Thickness(10, 6, 10, 6),
                Background = Brushes.SaddleBrown,
                Foreground = Brushes.White
            };

            cancelButton.Click += (s, e) =>
                dialog.DialogResult = false;

            addButton.Click += (s, e) =>
                dialog.DialogResult = true;

            buttons.Children.Add(cancelButton);
            buttons.Children.Add(addButton);

            panel.Children.Add(buttons);
            dialog.Content = panel;

            bool? result = dialog.ShowDialog();

            return result == true
                ? noteBox.Text.Trim()
                : null;
        }

        // Hộp thoại xác nhận thanh toán QR.
        private bool ShowQrPaymentDialog(decimal totalAmount)
        {
            var qrWindow = new Window
            {
                Title = "Thanh toán qua Mã QR VietQR",
                Width = 360,
                Height = 440,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this),
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#F7F5F0"))
            };

            var stackPanel = new StackPanel
            {
                Margin = new Thickness(15)
            };

            stackPanel.Children.Add(new TextBlock
            {
                Text = $"Tổng tiền: {totalAmount:N0} đ",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#C62828")),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            });

            string localImagePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "img",
                "QR.jpg");

            var qrImage = new Image
            {
                Width = 240,
                Height = 240,
                Source = new BitmapImage(
                    new Uri(localImagePath, UriKind.Absolute)),
                Margin = new Thickness(0, 0, 0, 15)
            };

            stackPanel.Children.Add(qrImage);

            var confirmButton = new Button
            {
                Content = "Đã nhận tiền / In hóa đơn",
                MinHeight = 40,
                Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#4E342E")),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                Cursor = System.Windows.Input.Cursors.Hand,
                BorderThickness = new Thickness(0)
            };

            confirmButton.Click += (s, e) =>
            {
                qrWindow.DialogResult = true;
            };

            stackPanel.Children.Add(confirmButton);
            qrWindow.Content = stackPanel;

            return qrWindow.ShowDialog() == true;
        }

        // Hiển thị thông báo từ ViewModel.
        private void ShowMessage(
            string message,
            string title,
            bool isError)
        {
            MessageBox.Show(
                message,
                title,
                MessageBoxButton.OK,
                isError
                    ? MessageBoxImage.Error
                    : MessageBoxImage.Information);
        }

        // Chuyển sang màn hình KDS.
        private void NavigateToKds()
        {
            if (Window.GetWindow(this) is MainShellWindow mainWindow)
            {
                mainWindow.OpenKds();
            }
        }
    }
}