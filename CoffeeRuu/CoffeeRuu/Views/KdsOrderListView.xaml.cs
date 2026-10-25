
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using CoffeeRuu.ViewModels;
using System;

namespace CoffeeRuu.Views
{
    public partial class KdsOrderListView : Page
    {


        public KdsOrderListView()
        {
            InitializeComponent();
        }


        private void EnterEmployeeId_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (DataContext is not KdsViewModel vm)
                return;

            // Tạo cửa sổ nhập mã nhân viên
            var dialog = new Window
            {
                Title = "Nhập EmployeeID",
                Width = 350,
                Height = 210,
                WindowStartupLocation =
                    WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this),
                ResizeMode = ResizeMode.NoResize
            };

            var panel = new StackPanel
            {
                Margin = new Thickness(20)
            };

            panel.Children.Add(new TextBlock
            {
                Text = "Mã nhân viên:",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 10)
            });

            // Ô nhập EmployeeID
            var employeeTextBox = new TextBox
            {
                Text = vm.EmployeeIdText,
                Height = 35,
                Padding = new Thickness(8, 5, 8, 5)
            };

            panel.Children.Add(employeeTextBox);

            // Khu vực nút
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment =
                    HorizontalAlignment.Right,
                Margin = new Thickness(0, 20, 0, 0)
            };

            var confirmButton = new Button
            {
                Content = "Xác nhận",
                Width = 100,
                Height = 35,
                IsDefault = true
            };

            var cancelButton = new Button
            {
                Content = "Hủy",
                Width = 80,
                Height = 35,
                IsCancel = true,
                Margin = new Thickness(10, 0, 0, 0)
            };

            confirmButton.Click += (s, args) =>
            {
                // Kiểm tra mã nhân viên
                if (!int.TryParse(
                    employeeTextBox.Text, out int employeeId)
                    || employeeId <= 0)
                {
                    MessageBox.Show(
                        dialog,
                        "EmployeeID phải là số nguyên dương.",
                        "Thông báo",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                // Lưu mã nhân viên vào ViewModel
                vm.EmployeeIdText = employeeId.ToString();

                dialog.DialogResult = true;
            };

            cancelButton.Click += (s, args) =>
            {
                dialog.DialogResult = false;
            };

            buttonPanel.Children.Add(confirmButton);
            buttonPanel.Children.Add(cancelButton);

            panel.Children.Add(buttonPanel);
            dialog.Content = panel;

            dialog.Loaded += (s, args) =>
                employeeTextBox.Focus();

            dialog.ShowDialog();
        }


        private void Page_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            if (DataContext is not KdsViewModel vm)
                return;

            // Lấy danh sách món từ ViewModel
            var view = CollectionViewSource
                .GetDefaultView(vm.Items);

            if (view == null)
                return;

            // Tránh tạo nhóm trùng khi Page tải lại
            bool hasGrouping = view.GroupDescriptions
                .OfType<PropertyGroupDescription>()
                .Any(g =>
                    g.PropertyName ==
                    nameof(KdsItemRow.OrderCode));

            if (hasGrouping)
                return;

            // Gom tất cả món có cùng mã đơn
            view.GroupDescriptions.Clear();

            view.GroupDescriptions.Add(
                new PropertyGroupDescription(
                    nameof(KdsItemRow.OrderCode)));
        }
    }
}
