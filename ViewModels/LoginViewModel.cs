using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using HillsCafeManagement.Helpers;
using HillsCafeManagement.Models;
using HillsCafeManagement.Services;
using HillsCafeManagement.Views.Admin;

namespace HillsCafeManagement.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        // Fields
        private string? _email;
        private string? _password;
        private string? _errorMessage;

        // Properties
        public string? Email
        {
            get => _email;
            set
            {
                _email = value;
                OnPropertyChanged();
            }
        }

        public string? Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged();
            }
        }

        public string? ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                OnPropertyChanged();
            }
        }

        public ICommand LoginCommand { get; }

        // Services
        private readonly DatabaseService _dbService;

        // Constructor
        public LoginViewModel()
        {
            _dbService = new DatabaseService();
            LoginCommand = new RelayCommand(Login);
        }

        // Login Method
        private void Login(object? parameter)
        {
            ErrorMessage = string.Empty;

            var emailInput = Email?.Trim() ?? string.Empty;
            var passwordInput = Password ?? string.Empty;

            var employee = _dbService.Login(emailInput, passwordInput);

            if (employee != null)
            {
                // Login successful
                ErrorMessage = string.Empty;

                var dashboardWindow = new DashboardWindow();
                dashboardWindow.Show();

                // Close the login window if it's passed in as a parameter
                if (parameter is Window loginWindow)
                {
                    loginWindow.Close();
                }
            }
            else
            {
                // Login failed
                ErrorMessage = "Invalid email or password.";
            }
        }

        // Property Changed Implementation
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
