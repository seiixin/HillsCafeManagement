using HillsCafeManagement.Models;
using HillsCafeManagement.Services;
using HillsCafeManagement.ViewModels;
using HillsCafeManagement.Views.Admin.Payrolls;
using System.Windows;
using System.Windows.Controls;

namespace HillsCafeManagement.Views.Admin.Payroll
{
    public partial class Payroll : UserControl
    {
        private PayrollViewModel viewModel;
        private readonly PayrollService payrollService;

        public Payroll()
        {
            InitializeComponent();
            payrollService = new PayrollService();
            viewModel = new PayrollViewModel();

            DataContext = viewModel;

            LoadPayrolls();
        }

        private void LoadPayrolls()
        {
            var payrolls = payrollService.GetAllPayrolls();
            viewModel.LoadPayrolls(payrolls);
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                string filter = textBox.Text.ToLower();
                viewModel.FilterPayroll(filter);
            }
        }

        // Method to be called from UI DeleteCommand in ViewModel
        public bool DeletePayroll(PayrollModel payroll)
        {
            if (payroll == null)
                return false;

            var result = MessageBox.Show(
                $"Are you sure you want to delete payroll record for employee ID {payroll.EmployeeId}?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                bool success = payrollService.DeletePayrollById(payroll.Id);

                if (success)
                {
                    MessageBox.Show("Payroll record deleted successfully.", "Deleted", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadPayrolls(); // Refresh list after deletion
                    return true;
                }
                else
                {
                    MessageBox.Show("Failed to delete payroll record.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            return false;
        }
        private void AddPayroll_Click(object sender, RoutedEventArgs e)
        {
            var addPayrollPopup = new AddEditPayroll();

            addPayrollPopup.OnPayrollSaved += () =>
            {
                LoadPayrolls(); // Refresh payroll list after save
            };

            RootGrid.Children.Add(addPayrollPopup);
        }
        private void EditPayroll_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is PayrollModel payroll)
            {
                var editPayrollPopup = new AddEditPayroll(payroll); // pass the model to edit

                editPayrollPopup.OnPayrollSaved += () =>
                {
                    LoadPayrolls(); // refresh list
                };

                RootGrid.Children.Add(editPayrollPopup);
            }
        }

    }
}
