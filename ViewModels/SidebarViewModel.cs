using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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

namespace HillsCafeManagement.ViewModels
{
    public class SidebarViewModel : INotifyPropertyChanged
    {
        private readonly UserModel _user;              // <- keep the truth you were injected
        private readonly string _role;                 // cache normalized role
        private readonly int _employeeId;              // 0 if none

        private string _userRole = string.Empty;
        private string _userName = string.Empty;
        private string _selectedMenuItem = string.Empty;
        private UserControl _currentView;

        public SidebarViewModel(UserModel user)
        {
            _user = user ?? throw new ArgumentNullException(nameof(user));
            _role = (_user.Role ?? string.Empty).Trim().ToUpperInvariant();
            _employeeId = _user.Employee?.Id ?? 0;

            UserRole = _role;
            UserName = _user.Employee?.FullName ?? _user.Email ?? "User";

            MenuItems = new ObservableCollection<string>();
            NavigateCommand = new RelayCommand<string?>(Navigate);

            InitializeMenuItems();
            SetDefaultView();
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
                if (_selectedMenuItem == value) return;
                _selectedMenuItem = value ?? string.Empty;
                OnPropertyChanged();
                if (!string.IsNullOrWhiteSpace(_selectedMenuItem))
                    Navigate(_selectedMenuItem);
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

        public ObservableCollection<string> MenuItems { get; }

        public ICommand NavigateCommand { get; }

        private void InitializeMenuItems()
        {
            MenuItems.Clear();

            switch (_role)
            {
                case "ADMIN":
                    MenuItems.Add("Dashboard");
                    MenuItems.Add("Users");
                    MenuItems.Add("Employees");
                    MenuItems.Add("Payroll");
                    MenuItems.Add("Payslip Requests");
                    MenuItems.Add("Attendance");
                    MenuItems.Add("Menu");
                    MenuItems.Add("Inventory");
                    MenuItems.Add("Orders");
                    MenuItems.Add("Receipts");
                    MenuItems.Add("Tables");
                    MenuItems.Add("Sales & Reports");
                    MenuItems.Add("Logout");
                    break;

                case "CASHIER":
                    MenuItems.Add("POS");
                    MenuItems.Add("Inventory");
                    MenuItems.Add("Orders");
                    MenuItems.Add("Receipts");
                    MenuItems.Add("Tables");
                    MenuItems.Add("Logout");
                    break;

                case "EMPLOYEE":
                    MenuItems.Add("Attendance");
                    MenuItems.Add("Payslip");
                    MenuItems.Add("Profile");
                    MenuItems.Add("Logout");
                    break;

                default:
                    MessageBox.Show("Unrecognized role: " + _role);
                    break;
            }
        }

        private void SetDefaultView()
        {
            switch (_role)
            {
                case "ADMIN":
                    CurrentView = new Dashboard();
                    break;

                case "CASHIER":
                    CurrentView = new POSView();
                    break;

                case "EMPLOYEE":
                    // Pass the actual employee id; if invalid, show a friendly stub view
                    CurrentView = MakeEmployeeProfileViewOrFallback();
                    break;

                default:
                    CurrentView = new Dashboard();
                    break;
            }
        }

        public void Navigate(string? menuItem)
        {
            switch (menuItem?.Trim().ToLowerInvariant())
            {
                // ADMIN
                case "dashboard":
                    if (IsAdmin) CurrentView = new Dashboard();
                    break;

                case "users":
                    if (IsAdmin) CurrentView = new Users();
                    break;

                case "employees":
                    if (IsAdmin) CurrentView = new Employees();
                    break;

                case "payroll":
                    if (IsAdmin) CurrentView = new Payroll();
                    break;

                case "payslip requests":
                    if (IsAdmin) CurrentView = new Payslip();
                    break;

                case "attendance":
                    if (IsAdmin) CurrentView = new AttendanceAdminView();
                    else if (IsEmployee) CurrentView = new AttendanceView();
                    break;

                case "menu":
                    if (IsAdmin) CurrentView = new MenuView();
                    break;

                case "inventory":
                    if (IsAdmin) CurrentView = new Inventory();
                    else if (IsCashier) CurrentView = new InventoryView();
                    break;

                case "orders":
                    if (IsAdmin) CurrentView = new Orders();
                    else if (IsCashier) CurrentView = new OrdersView();
                    break;

                case "receipts":
                    if (IsAdmin) CurrentView = new Receipts();
                    else if (IsCashier) CurrentView = new ReceiptsView();
                    break;

                case "tables":
                    if (IsAdmin) CurrentView = new Tables();
                    else if (IsCashier) CurrentView = new TablesView();
                    break;

                case "sales & reports":
                    if (IsAdmin) CurrentView = new Sales();
                    break;

                // CASHIER
                case "pos":
                    if (IsCashier) CurrentView = new POSView();
                    break;

                // EMPLOYEE
                case "payslip":
                    if (IsEmployee) CurrentView = new PayslipView();
                    break;

                case "profile":
                    if (IsEmployee) CurrentView = MakeEmployeeProfileViewOrFallback();
                    break;

                case "logout":
                    LogoutToMainWindow();
                    break;
            }
        }

        private bool IsAdmin => _role == "ADMIN";
        private bool IsCashier => _role == "CASHIER";
        private bool IsEmployee => _role == "EMPLOYEE";

        private UserControl MakeEmployeeProfileViewOrFallback()
        {
            if (_employeeId > 0)
            {
                // Use the strong constructor that triggers VM load via EmployeeId.
                return new ProfileView(_employeeId);
            }

            // Nice fallback: show a lightweight message view instead of silently showing someone else’s profile.
            var border = new Border
            {
                Padding = new Thickness(24),
                Child = new TextBlock
                {
                    Text = "No employee record was found for the current user.",
                    FontSize = 16,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            return new UserControl { Content = border };
        }

        private static void LogoutToMainWindow()
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();

            foreach (Window window in Application.Current.Windows)
            {
                if (window != mainWindow)
                {
                    window.Close();
                    // Close only the current shell; keep iter simple
                    break;
                }
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
