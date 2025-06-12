// File: ViewModels/SidebarViewModel.cs
// Admin Views
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
// Cashier Views
using HillsCafeManagement.Views.Cashier.Inventory;
using HillsCafeManagement.Views.Cashier.Orders;
using HillsCafeManagement.Views.Cashier.POS;
using HillsCafeManagement.Views.Cashier.Receipts;
using HillsCafeManagement.Views.Cashier.Tables;
// Employee Views
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
        private string _userRole;
        private string _userName;
        private string _selectedMenuItem;
        private UserControl _currentView;

        public SidebarViewModel(string userRole, string userName)
        {
            UserRole = userRole.ToUpper();
            UserName = userName;
            MenuItems = new ObservableCollection<string>();
            NavigateCommand = new RelayCommand<string>(Navigate);

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
                if (value != null)
                {
                    _currentView = value;
                    OnPropertyChanged();
                }
                else
                {
                    // log error or fallback view
                    Console.WriteLine("Warning: CurrentView was set to null.");
                }
            }
        }

        public ObservableCollection<string> MenuItems { get; set; }
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
            }
        }

        public void Navigate(string menuItem)
        {
            switch (menuItem?.ToLower())
            {
                // === ADMIN VIEWS ===
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
                    if (UserRole == "ADMIN") CurrentView = new Attendance();
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

                // === CASHIER VIEWS ===
                case "pos":
                    if (UserRole == "CASHIER") CurrentView = new POSView();
                    break;

                // === EMPLOYEE VIEWS ===
                case "payslip":
                    if (UserRole == "EMPLOYEE") CurrentView = new PayslipView();
                    break;
                case "profile":
                    if (UserRole == "EMPLOYEE") CurrentView = new ProfileView();
                    break;

                // === SHARED ===
                case "logout":
                    // Open a new MainWindow (login screen or landing window)
                    var mainWindow = new MainWindow();
                    mainWindow.Show();

                    // Close the current window
                    foreach (Window window in Application.Current.Windows)
                    {
                        if (window != mainWindow)
                        {
                            window.Close();
                            break; // Only close the current window, not MainWindow
                        }
                    }
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

    // Reusable RelayCommand class
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