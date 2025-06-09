// File Location: ViewModels/SidebarViewModel.cs
using HillsCafeManagement.Views.Admin.Attendance;
using HillsCafeManagement.Views.Admin.Dashboard;
using HillsCafeManagement.Views.Admin.Employees;
using HillsCafeManagement.Views.Admin.Inventory;
using HillsCafeManagement.Views.Admin.Orders;
using HillsCafeManagement.Views.Admin.Payroll;
using HillsCafeManagement.Views.Admin.Payslip_Requests;
using HillsCafeManagement.Views.Admin.Receipts;
using HillsCafeManagement.Views.Admin.Tables;
using HillsCafeManagement.Views.Admin.Users;
using HillsCafeManagement.Views.Admin.Menu;
using HillsCafeManagement.Views.Admin.Sales;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Input;

namespace HillsCafeManagement.ViewModels
{
    public class SidebarViewModel : INotifyPropertyChanged
    {
        private string _userRole;
        private string _userName;
        private string _selectedMenuItem;
        private UserControl _currentView;

        public SidebarViewModel()
        {
            // Initialize default values
            UserRole = "ADMIN";
            UserName = "(First Last)";

            // Initialize menu items
            MenuItems = new ObservableCollection<string>
            {
                "Dashboard",
                "Users",
                "Employees",
                "Payroll",
                "Payslip Requests",
                "Attendance",
                "Menu",
                "Inventory",
                "Orders",
                "Receipts",
                "Tables",
                "Sales & Reports",
                "Logout"
            };

            // Initialize commands
            NavigateCommand = new RelayCommand<string>(Navigate);

            // Set default view
            CurrentView = new Dashboard();
        }

        public string UserRole
        {
            get => _userRole;
            set
            {
                _userRole = value;
                OnPropertyChanged();
            }
        }

        public string UserName
        {
            get => _userName;
            set
            {
                _userName = value;
                OnPropertyChanged();
            }
        }

        public string SelectedMenuItem
        {
            get => _selectedMenuItem;
            set
            {
                _selectedMenuItem = value;
                OnPropertyChanged();
                // Handle navigation when item is selected
                if (!string.IsNullOrEmpty(value))
                {
                    Navigate(value);
                }
            }
        }

        public UserControl CurrentView
        {
            get => _currentView;
            set
            {
                _currentView = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> MenuItems { get; set; }

        public ICommand NavigateCommand { get; }

        public void Navigate(string menuItem)
        {
            switch (menuItem?.ToLower())
            {
                case "dashboard":
                    CurrentView = new Dashboard();
                    break;
                case "users":
                    CurrentView = new Users();
                    break;
                case "employees":
                    CurrentView = new Employees();
                    break;
                case "payroll":
                    CurrentView = new Payroll();
                    break;
                case "payslip requests":
                    CurrentView = new Payslip();
                    break;
                case "attendance":
                    CurrentView = new Attendance();
                    break;
                case "menu":
                    CurrentView = new MenuView();
                    break;
                case "inventory":
                    CurrentView = new Inventory();
                    break;
                case "orders":
                    CurrentView = new Orders();
                    break;
                case "receipts":
                    CurrentView = new Receipts();
                    break;
                case "tables":
                    CurrentView = new Tables();
                    break;
                case "sales & reports":
                    CurrentView = new Sales();
                    break;
                case "logout":
                    // TODO: Handle logout logic
                    break;
                default:
                    CurrentView = null;
                    break;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Simple RelayCommand implementation
    public class RelayCommand<T> : ICommand
    {
        private readonly System.Action<T> _execute;
        private readonly System.Func<T, bool> _canExecute;

        public RelayCommand(System.Action<T> execute, System.Func<T, bool> canExecute = null)
        {
            _execute = execute ?? throw new System.ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute((T)parameter);
        }

        public void Execute(object parameter)
        {
            _execute((T)parameter);
        }

        public event System.EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}