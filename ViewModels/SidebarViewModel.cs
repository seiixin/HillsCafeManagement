using HillsCafeManagement.Models;
using HillsCafeManagement.Views.Admin.Attendance;
using HillsCafeManagement.Views.Admin.Dashboard;
using HillsCafeManagement.Views.Admin.Employees;
using HillsCafeManagement.Views.Admin.Inventory;
using HillsCafeManagement.Views.Admin.Menu;
using HillsCafeManagement.Views.Admin.Orders;
using HillsCafeManagement.Views.Admin.Payroll;
using HillsCafeManagement.Views.Admin.Payslip_Requests;
using HillsCafeManagement.Views.Admin.Receipts;
using HillsCafeManagement.Views.Admin.Sales;
using HillsCafeManagement.Views.Admin.Tables;
using HillsCafeManagement.Views.Admin.Users;
using HillsCafeManagement.Views.Cashier.Inventory;
using HillsCafeManagement.Views.Cashier.Orders;
using HillsCafeManagement.Views.Cashier.POS;
using HillsCafeManagement.Views.Cashier.Receipts;
using HillsCafeManagement.Views.Cashier.Tables;
using HillsCafeManagement.Views.Employee.Attendance;
using HillsCafeManagement.Views.Employee.Payslip;
using HillsCafeManagement.Views.Employee.Profile;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HillsCafeManagement.ViewModels
{
    public class SidebarViewModel : INotifyPropertyChanged
    {
        private string _userRole = string.Empty;
        private string _userName = string.Empty;
        private string _selectedMenuItem = string.Empty;
        private UserControl _currentView = new Dashboard(); // or null if set in constructor

        public SidebarViewModel(UserModel user)
        {
            UserRole = user.Role!.ToUpper(); // tells compiler: “I promise it's not null”
            UserName = user.Employee?.FullName ?? user.Email!;
            MenuItems = new ObservableCollection<string>();
            NavigateCommand = new RelayCommand<string?>(Navigate);

            InitializeMenuItems();
        }

        public string UserRole
        {
            get => _userRole;
            set { _userRole = value; OnPropertyChanged(); }
        }

        public string UserName
        {
            get => _userName;
            set { _userName = value; OnPropertyChanged(); }
        }

        public string SelectedMenuItem
        {
            get => _selectedMenuItem;
            set
            {
                _selectedMenuItem = value;
                OnPropertyChanged();
                if (!string.IsNullOrWhiteSpace(value))
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
                _currentView = value ?? throw new ArgumentNullException(nameof(CurrentView));
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> MenuItems { get; private set; } = new();

        public ICommand NavigateCommand { get; }

        private void InitializeMenuItems()
        {
            switch (UserRole)
            {
                case "ADMIN":
                    MenuItems = new ObservableCollection<string>
                    {
                        "Dashboard", "Users", "Employees", "Payroll", "Payslip Requests",
                        "Attendance", "Menu", "Inventory", "Orders", "Receipts",
                        "Tables", "Sales & Reports", "Logout"
                    };
                    CurrentView = new Dashboard();
                    break;

                case "CASHIER":
                    MenuItems = new ObservableCollection<string>
                    {
                        "POS", "Inventory", "Orders", "Receipts", "Tables", "Logout"
                    };
                    CurrentView = new POSView();
                    break;

                case "EMPLOYEE":
                    MenuItems = new ObservableCollection<string>
                    {
                        "Attendance", "Payslip", "Profile", "Logout"
                    };
                    CurrentView = new ProfileView();
                    break;

                default:
                    MessageBox.Show("Unrecognized role: " + UserRole);
                    break;
            }
        }

        public void Navigate(string? menuItem)
        {
            switch (menuItem?.ToLower())
            {
                case "dashboard":
                    if (UserRole == "ADMIN") CurrentView = new Dashboard();
                    break;
                case "users":
                    if (UserRole == "ADMIN") CurrentView = new Users();
                    break;
                case "employees":
                    if (UserRole == "ADMIN") CurrentView = new Employees();
                    break;
                case "payroll":
                    if (UserRole == "ADMIN") CurrentView = new Payroll();
                    break;
                case "payslip requests":
                    if (UserRole == "ADMIN") CurrentView = new Payslip();
                    break;
                case "attendance":
                    if (UserRole == "ADMIN") CurrentView = new AttendanceAdminView();
                    else if (UserRole == "EMPLOYEE") CurrentView = new AttendanceView();
                    break;
                case "menu":
                    if (UserRole == "ADMIN") CurrentView = new MenuView();
                    break;
                case "inventory":
                    if (UserRole == "ADMIN") CurrentView = new Inventory();
                    else if (UserRole == "CASHIER") CurrentView = new InventoryView();
                    break;
                case "orders":
                    if (UserRole == "ADMIN") CurrentView = new Orders();
                    else if (UserRole == "CASHIER") CurrentView = new OrdersView();
                    break;
                case "receipts":
                    if (UserRole == "ADMIN") CurrentView = new Receipts();
                    else if (UserRole == "CASHIER") CurrentView = new ReceiptsView();
                    break;
                case "tables":
                    if (UserRole == "ADMIN") CurrentView = new Tables();
                    else if (UserRole == "CASHIER") CurrentView = new TablesView();
                    break;
                case "sales & reports":
                    if (UserRole == "ADMIN") CurrentView = new Sales();
                    break;
                case "pos":
                    if (UserRole == "CASHIER") CurrentView = new POSView();
                    break;
                case "payslip":
                    if (UserRole == "EMPLOYEE") CurrentView = new PayslipView();
                    break;
                case "profile":
                    if (UserRole == "EMPLOYEE") CurrentView = new ProfileView();
                    break;
                case "logout":
                    var mainWindow = new MainWindow();
                    mainWindow.Show();

                    foreach (Window window in Application.Current.Windows)
                    {
                        if (window != mainWindow)
                        {
                            window.Close();
                            break;
                        }
                    }
                    break;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Func<T?, bool>? _canExecute;

        public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) =>
            _canExecute == null || _canExecute((T?)parameter);

        public void Execute(object? parameter) =>
            _execute((T?)parameter);

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}
