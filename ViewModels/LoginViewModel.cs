using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using HillsCafeManagement.Helpers;
using HillsCafeManagement.Models;
using HillsCafeManagement.Services;
using HillsCafeManagement.Views.Layouts;
using HillsCafeManagement.ViewModels;

namespace HillsCafeManagement.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private string? _email;
        private string? _password;
        private string? _errorMessage;

        public string? Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(); }
        }

        public string? Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(); }
        }

        public string? ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); }
        }

        public ICommand LoginCommand { get; }

        private readonly DatabaseService _dbService;

        public LoginViewModel()
        {
            _dbService = new DatabaseService();
            LoginCommand = new RelayCommand(Login);
        }

        private void Login(object? parameter)
        {
            ErrorMessage = string.Empty;

            var emailInput = Email?.Trim() ?? string.Empty;
            var passwordInput = Password ?? string.Empty;

            var employee = _dbService.Login(emailInput, passwordInput);

            if (employee != null)
            {
                // Extract user role from the Position field in the Employee table
                string userRole = string.IsNullOrWhiteSpace(employee.Position) ? "EMPLOYEE" : employee.Position.ToUpper();
                string userName = employee.FullName;

                // Create and configure main layout with SidebarViewModel
                var mainLayout = new MainLayout
                {
                    DataContext = new SidebarViewModel(userRole, userName)
                };

                var window = new Window
                {
                    Title = "Dashboard",
                    Content = mainLayout,
                    Width = 1024,
                    Height = 768,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };
                window.Show();

                if (parameter is Window loginWindow)
                {
                    loginWindow.Close();
                }

                Console.WriteLine($"Login attempt: Email = {emailInput}, Password = {passwordInput}");
                if (employee != null)
                {
                    Console.WriteLine($"Login success: Role = {employee.Position}, Name = {employee.FullName}");
                }
                else
                {
                    Console.WriteLine("Login failed: Invalid credentials.");
                }

            }
            else
            {
                ErrorMessage = "Invalid email or password.";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
