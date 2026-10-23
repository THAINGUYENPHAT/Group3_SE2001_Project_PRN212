
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
            if (DataContext is EmployeeManagementViewModel vm)
            {
                string password = PasswordInput.Password;

                if (vm.Password != password)
                {
                    vm.Password = password;
                }
            }
        }

        private void PasswordInput_IsVisibleChanged(
            object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!PasswordInput.IsVisible)
            {
                PasswordInput.Clear();

                if (_viewModel.Password != "")
                {
                    _viewModel.Password = "";
                }
            }
        }

        private void ViewModel_PropertyChanged(
            object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName ==
                nameof(EmployeeManagementViewModel.Password))
            {
                if (PasswordInput.Password != _viewModel.Password)
                {
                    PasswordInput.Password = _viewModel.Password;
                }
            }
        }
    }
}
