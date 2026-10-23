
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using CoffeeRuu.Services;
using CoffeeRuu.ViewModels;

namespace CoffeeRuu.Views
{
    public partial class EmployeeManagementView : UserControl
    {
        private readonly EmployeeManagementViewModel _viewModel;
        private bool _isPasswordVisible;

        public EmployeeManagementView()
        {
            InitializeComponent();

            _viewModel = new EmployeeManagementViewModel(
                new EmployeeService());

            DataContext = _viewModel;

            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void PasswordInput_PasswordChanged(
            object sender, RoutedEventArgs e)
        {
            if (_isPasswordVisible)
            {
                return;
            }

            if (DataContext is EmployeeManagementViewModel vm)
            {
                if (vm.Password != PasswordInput.Password)
                {
                    vm.Password = PasswordInput.Password;
                }
            }
        }

        private void PasswordTextInput_TextChanged(
            object sender, TextChangedEventArgs e)
        {
            if (!_isPasswordVisible)
            {
                return;
            }

            if (DataContext is EmployeeManagementViewModel vm)
            {
                if (vm.Password != PasswordTextInput.Text)
                {
                    vm.Password = PasswordTextInput.Text;
                }
            }
        }

        private void TogglePasswordButton_Click(
            object sender, RoutedEventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;

            if (_isPasswordVisible)
            {
                PasswordTextInput.Text = PasswordInput.Password;

                PasswordInput.Visibility = Visibility.Collapsed;
                PasswordTextInput.Visibility = Visibility.Visible;

                PasswordTextInput.Focus();
                PasswordTextInput.CaretIndex =
                    PasswordTextInput.Text.Length;

                TogglePasswordButton.Content = "🙈";
            }
            else
            {
                PasswordInput.Password = PasswordTextInput.Text;

                PasswordTextInput.Visibility = Visibility.Collapsed;
                PasswordInput.Visibility = Visibility.Visible;

                PasswordInput.Focus();

                TogglePasswordButton.Content = "👁";
            }
        }

        private void ViewModel_PropertyChanged(
            object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName ==
                nameof(EmployeeManagementViewModel.Password))
            {
                string password = _viewModel.Password ?? "";

                if (PasswordInput.Password != password)
                {
                    PasswordInput.Password = password;
                }

                if (PasswordTextInput.Text != password)
                {
                    PasswordTextInput.Text = password;
                }
            }
        }
    }
}
