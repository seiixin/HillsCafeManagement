﻿using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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
using HillsCafeManagement.Views.Employee.Payroll; // ✅ NEW (PayrollRecords)

// alias the service types (same technique as your EmployeeProfileViewModel)
using IEmployeeService = HillsCafeManagement.Services.IEmployeeService;
using EmployeeService = HillsCafeManagement.Services.EmployeeService;

namespace HillsCafeManagement.ViewModels
{
    public class SidebarViewModel : INotifyPropertyChanged
    {
        // -----------------------------
        // Dependencies / identity
        // -----------------------------
        private readonly IEmployeeService _service;
        private int _employeeId;             // authoritative id used for DB fetch
        private string _role = string.Empty; // normalized role from DB

        // -----------------------------
        // Header state (bound in XAML)
        // -----------------------------
        private string _userRole = string.Empty;
        private string _userName = string.Empty;
        private ImageSource? _userAvatarImage;

        // -----------------------------
        // UI state
        // -----------------------------
        private string _selectedMenuItem = string.Empty;
        private UserControl _currentView = new UserControl();

        public SidebarViewModel(int employeeId, IEmployeeService? service = null)
        {
            _service = service ?? new EmployeeService();
            MenuItems = new ObservableCollection<string>();
            NavigateCommand = new RelayCommand<string?>(Navigate);
            ReloadProfileCommand = new RelayCommand<object?>(_ => LoadEmployee(_employeeId));
            RefreshAvatarCommand = new RelayCommand<object?>(_ => RefreshAvatar());

            _employeeId = employeeId;
            LoadEmployee(_employeeId); // pulls name/role/avatar from DB and sets menu + default view
        }

        // Convenience overload: allow constructing with a UserModel too
        public SidebarViewModel(UserModel user, IEmployeeService? service = null)
            : this(user?.Employee?.Id ?? 0, service)
        { }

        // -----------------------------
        // Bindable properties
        // -----------------------------
        public string UserRole
        {
            get => _userRole;
            private set { _userRole = value; OnPropertyChanged(); }
        }

        public string UserName
        {
            get => _userName;
            private set { _userName = value; OnPropertyChanged(); }
        }

        public ImageSource? UserAvatarImage
        {
            get => _userAvatarImage;
            private set { _userAvatarImage = value; OnPropertyChanged(); }
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
            set { _currentView = value ?? throw new ArgumentNullException(nameof(CurrentView)); OnPropertyChanged(); }
        }

        public ObservableCollection<string> MenuItems { get; }
        public ICommand NavigateCommand { get; }
        public ICommand ReloadProfileCommand { get; }
        public ICommand RefreshAvatarCommand { get; }

        // -----------------------------
        // Data fetch (same approach as EmployeeProfileViewModel)
        // -----------------------------
        private void LoadEmployee(int id)
        {
            if (id <= 0)
            {
                // fallback visuals (e.g., in designer or if no employee mapped)
                ApplyHeader(null);
                InitializeMenuItems(); // with whatever _role currently is (may be empty)
                SetDefaultView();
                return;
            }

            try
            {
                var one = _service.GetEmployeeById(id);
                if (one == null)
                {
                    // gentle fallback if service shape differs
                    var list = _service.GetAllEmployees();
                    one = list.Find(e => e.Id == id);
                }

                ApplyHeader(one);
                InitializeMenuItems();
                SetDefaultView();
            }
            catch (Exception ex)
            {
                // minimal error surfacing in sidebar; you can expand if desired
                ApplyHeader(null);
                InitializeMenuItems();
                SetDefaultView();
                System.Diagnostics.Debug.WriteLine($"Sidebar LoadEmployee failed: {ex.Message}");
            }
        }

        private void ApplyHeader(EmployeeModel? emp)
        {
            // Derive name
            UserName = string.IsNullOrWhiteSpace(emp?.FullName)
                ? "(Unknown)"
                : emp!.FullName;

            // Derive role from linked user account (fallback to previous or "EMPLOYEE")
            var roleRaw = emp?.UserAccount?.Role ?? _role;
            _role = (roleRaw ?? "EMPLOYEE").Trim().ToUpperInvariant();
            UserRole = _role;

            // Derive avatar
            var path = emp?.ImageUrl;
            UserAvatarImage = LoadImageOrFallback(path);
        }

        private static ImageSource LoadImageOrFallback(string? path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                    return MakePlaceholderAvatar();

                var uriKind = Uri.IsWellFormedUriString(path, UriKind.Absolute)
                    ? UriKind.Absolute
                    : UriKind.RelativeOrAbsolute;

                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(path, uriKind);
                bmp.CacheOption = BitmapCacheOption.OnLoad;           // avoid file locking
                bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
            catch
            {
                return MakePlaceholderAvatar();
            }
        }

        private static ImageSource MakePlaceholderAvatar()
        {
            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
                dc.DrawRectangle(Brushes.Gray, null, new Rect(0, 0, 64, 64));

            var rtb = new RenderTargetBitmap(64, 64, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(dv);
            rtb.Freeze();
            return rtb;
        }

        // allow external callers to just refresh the picture without a full DB roundtrip
        public void RefreshAvatar(string? newImagePath = null)
        {
            UserAvatarImage = LoadImageOrFallback(newImagePath);
        }

        // -----------------------------
        // Menu / Navigation
        // -----------------------------
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
                default:
                    MenuItems.Add("Attendance");
                    MenuItems.Add("Payslip");
                    MenuItems.Add("Payroll Records");   // ✅ NEW
                    MenuItems.Add("Profile");
                    MenuItems.Add("Logout");
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
                default:
                    CurrentView = MakeEmployeeProfileViewOrFallback();
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
                    else if (IsEmployee)
                        // IMPORTANT: pass the known employee id into the employee Attendance view
                        CurrentView = new AttendanceView(_employeeId);
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

                case "payroll records":                 // ✅ NEW
                    if (IsEmployee) CurrentView = new PayrollRecords(_employeeId);
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
                return new ProfileView(_employeeId);

            var border = new Border
            {
                Padding = new Thickness(24),
                Child = new System.Windows.Controls.TextBlock
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
                    break;
                }
            }
        }

        // -----------------------------
        // INotifyPropertyChanged
        // -----------------------------
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // your RelayCommand<T> stays the same
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Func<T?, bool>? _canExecute;

        public RelayCommand(Action<T?> execute, Func<T?, bool>? _canExecute = null)
        {
            this._execute = execute ?? throw new ArgumentNullException(nameof(execute));
            this._canExecute = _canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute == null || _canExecute((T?)parameter);
        public void Execute(object? parameter) => _execute((T?)parameter);

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}
