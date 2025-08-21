using System;
using System.Collections.Generic;
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
        private readonly PayslipService _payslipService = new();
        private readonly PayrollViewModel _vm = new();

        public Payroll()
        {
            InitializeComponent();
            DataContext = _vm;
            RefreshFromDatabase();
        }

        private void RefreshFromDatabase()
        {
            var data = _payrollService.GetAllPayrolls();
            _vm.LoadPayrolls(data);
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var filter = (sender as TextBox)?.Text ?? string.Empty;
            _vm.FilterPayroll(filter);
        }

        private void AddPayroll_Click(object sender, RoutedEventArgs e)
        {
            var editor = new AddEditPayroll();
            editor.OnPayrollSaved += RefreshFromDatabase;
            editor.CloseRequested += CloseOverlay;
            ShowOverlay(editor);
        }

        private void EditPayroll_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not PayrollModel row) return;

            var editor = new AddEditPayroll(row);
            editor.OnPayrollSaved += RefreshFromDatabase;
            editor.CloseRequested += CloseOverlay;
            ShowOverlay(editor);
        }

        public bool DeletePayrollById(int id)
        {
            var ok = _payrollService.DeletePayrollById(id);
            if (ok) RefreshFromDatabase();
            return ok;
        }

        private void ShowOverlay(UserControl editor)
        {
            var host = RootGrid.Children
                               .OfType<ContentControl>()
                               .FirstOrDefault(c => c.Name == "OverlayHost");
            if (host == null)
            {
                host = new ContentControl { Name = "OverlayHost" };
                Grid.SetRow(host, 3); // overlay above the DataGrid row
                RootGrid.Children.Add(host);
            }
            host.Content = editor;
        }

        private void CloseOverlay()
        {
            var host = RootGrid.Children
                               .OfType<ContentControl>()
                               .FirstOrDefault(c => c.Name == "OverlayHost");
            if (host != null) host.Content = null;
        }

        // ===== Generate Payroll =====
        private void GeneratePayroll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var now = DateTime.Now;
                var choice = ((PeriodCombo.SelectedItem as ComboBoxItem)?.Content as string) ?? "FirstHalf";

                DateTime start, end;

                if (string.Equals(choice, "Custom", StringComparison.OrdinalIgnoreCase))
                {
                    if (CustomStartPicker.SelectedDate is not DateTime s ||
                        CustomEndPicker.SelectedDate is not DateTime t)
                    {
                        MessageBox.Show("Please select Custom Start and End dates.", "Missing Dates",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    if (t.Date < s.Date)
                    {
                        MessageBox.Show("End date must be on/after Start date.", "Invalid Range",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    start = s.Date; end = t.Date;
                }
                else if (string.Equals(choice, "SecondHalf", StringComparison.OrdinalIgnoreCase))
                {
                    (start, end) = _payrollService.GetSecondHalfRange(now.Year, now.Month);
                }
                else
                {
                    (start, end) = _payrollService.GetFirstHalfRange(now.Year, now.Month);
                }

                // Generate + Save
                var generated = _payrollService.GenerateForPeriod(start, end);
                if (generated.Count == 0)
                {
                    MessageBox.Show($"No payroll rows generated for {start:yyyy-MM-dd} to {end:yyyy-MM-dd}.",
                        "Nothing to Save", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var saveOk = _payrollService.SaveGeneratedPayrolls(generated);
                if (!saveOk)
                {
                    MessageBox.Show("Failed to save generated payroll rows.", "Save Failed",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Optional: create payslips for this period if you added the method
                var createdSlips = _payslipService.CreatePayslipsFromPayrollPeriod(start, end);

                MessageBox.Show(
                    $"Generated payroll for {start:yyyy-MM-dd} → {end:yyyy-MM-dd}\n" +
                    $"Rows: {generated.Count}\n" +
                    $"Payslips created: {createdSlips}",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                RefreshFromDatabase();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating payroll:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
