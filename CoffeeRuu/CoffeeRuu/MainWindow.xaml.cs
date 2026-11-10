
using System.Windows;
using System.Windows.Controls;
using CoffeeRuu.Services;
using CoffeeRuu.ViewModels;

namespace CoffeeRuu
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var authenticationService =
                new AuthenticationService(App.CreateDbContext);

            var dialogService = new MessageDialogService();

            var windowService = new WindowService(this);

            DataContext = new LoginViewModel(
                authenticationService,
                dialogService,
                windowService);

            PasswordInput.PasswordChanged += PasswordInput_PasswordChanged;
        }

        private void PasswordInput_PasswordChanged(
            object sender,
            RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel viewModel)
            {
                viewModel.Password = PasswordInput.Password;
            }
        }

        private void Window_KeyDown(
            object sender,
            System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (DataContext is LoginViewModel viewModel
                    && viewModel.LoginCommand.CanExecute(null))
                {
                    viewModel.LoginCommand.Execute(null);
                }

                e.Handled = true;
            }
        }
    }
}
