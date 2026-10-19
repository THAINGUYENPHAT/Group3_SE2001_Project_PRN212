
using System.Windows;
using CoffeeRuu.ViewModels;

namespace CoffeeRuu.Views
{
    public partial class LoginView : Window
    {
        public LoginView()
        {
            InitializeComponent();
            DataContext = new LoginViewModel();

            PasswordInput.PasswordChanged += PasswordInput_PasswordChanged;
        }

        private void PasswordInput_PasswordChanged(
            object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel viewModel)
            {
                viewModel.Password = PasswordInput.Password;
            }
        }
    }
}
