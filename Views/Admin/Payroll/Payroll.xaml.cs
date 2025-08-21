using System.Linq;
using System.Windows;
using System.Windows.Controls;
using HillsCafeManagement.Models;
using HillsCafeManagement.Services;
using HillsCafeManagement.ViewModels;
using HillsCafeManagement.Views.Admin.Payrolls;

namespace HillsCafeManagement.Views.Admin.Payroll
{
    public partial class Payroll : UserControl
    {
        private readonly PayrollService _payrollService = new();
        private readonly PayrollViewModel _vm = new();

        public Payroll()
        {
            InitializeComponent();
            DataContext = _vm;
            RefreshFromDatabase();
        }

        // --- Load/Refresh ---
        private void RefreshFromDatabase()
        {
            var data = _payrollService.GetAllPayrolls();
            _vm.LoadPayrolls(data);
        }

        // --- Search box handler (hooked in XAML) ---
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var filter = (sender as TextBox)?.Text ?? string.Empty;
            _vm.FilterPayroll(filter);
        }

        // --- Add payroll (opens overlay) ---
        private void AddPayroll_Click(object sender, RoutedEventArgs e)
        {
            var editor = new AddEditPayroll();
            editor.OnPayrollSaved += RefreshFromDatabase;
            ShowOverlay(editor);
        }

        // --- Edit payroll (opens overlay) ---
        private void EditPayroll_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not PayrollModel row) return;

            var editor = new AddEditPayroll(row);
            editor.OnPayrollSaved += RefreshFromDatabase;
            ShowOverlay(editor);
        }

        // --- Optional helper if you later want VM to call into service for deletes ---
        public bool DeletePayrollById(int id)
        {
            var ok = _payrollService.DeletePayrollById(id);
            if (ok) RefreshFromDatabase();
            return ok;
        }

        // --- Lightweight overlay host inside the existing RootGrid ---
        private void ShowOverlay(UserControl editor)
        {
            // Reuse or create a simple overlay host on row 2
            var host = RootGrid.Children
                               .OfType<ContentControl>()
                               .FirstOrDefault(c => c.Name == "OverlayHost");
            if (host == null)
            {
                host = new ContentControl { Name = "OverlayHost" };
                Grid.SetRow(host, 2); // same row as the DataGrid area
                RootGrid.Children.Add(host);
            }

            host.Content = editor;
        }
    }
}
