using HillsCafeManagement.Models;
using HillsCafeManagement.Services;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace HillsCafeManagement.Views.Admin.Payrolls
{
    public partial class AddEditPayroll : UserControl
    {
        private readonly PayrollService _payrollService = new();
        private readonly EmployeeService _employeeService = new();

        private readonly bool _isEditMode;
        private readonly PayrollModel? _editingPayroll;

        public List<EmployeeModel> Employees { get; set; } = new();

        // Event to notify parent when saved
        public delegate void PayrollSavedHandler();
        public event PayrollSavedHandler? OnPayrollSaved;

        public AddEditPayroll(PayrollModel? payroll = null)
        {
            InitializeComponent();

            DataContext = this;

            Employees = _employeeService.GetAllEmployees();
            EmployeeComboBox.ItemsSource = Employees;

            if (payroll != null)
            {
                _isEditMode = true;
                _editingPayroll = payroll;

                TitleText.Text = "Edit Payroll";

                EmployeeComboBox.SelectedValue = payroll.EmployeeId;
                StartDatePicker.SelectedDate = payroll.StartDate;
                EndDatePicker.SelectedDate = payroll.EndDate;
                DaysWorkedTextBox.Text = payroll.TotalDaysWorked.ToString();
                GrossSalaryTextBox.Text = payroll.GrossSalary.ToString("0.00");
                SSSTextBox.Text = (payroll?.SssDeduction ?? 0).ToString("0.00");
                PhilhealthTextBox.Text = payroll.PhilhealthDeduction.ToString("0.00");
                PagibigTextBox.Text = payroll.PagibigDeduction.ToString("0.00");
                OtherDeductionsTextBox.Text = payroll.OtherDeductions.ToString("0.00");
                BonusTextBox.Text = payroll.Bonus.ToString("0.00");
                NetSalaryTextBox.Text = payroll.NetSalary.ToString("0.00");
                BranchNameTextBox.Text = payroll.BranchName;
                ShiftTypeTextBox.Text = payroll.ShiftType;
            }
            else
            {
                _isEditMode = false;
                TitleText.Text = "Add New Payroll";
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            var parent = this.Parent as Panel;
            parent?.Children.Remove(this);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (EmployeeComboBox.SelectedValue == null || !DateTime.TryParse(StartDatePicker.Text, out var startDate) || !DateTime.TryParse(EndDatePicker.Text, out var endDate))
            {
                MessageBox.Show("Please fill required fields.");
                return;
            }

            var payroll = new PayrollModel
            {
                EmployeeId = (int)EmployeeComboBox.SelectedValue,
                StartDate = startDate,
                EndDate = endDate,
                TotalDaysWorked = int.TryParse(DaysWorkedTextBox.Text, out var d) ? d : 0,
                GrossSalary = decimal.TryParse(GrossSalaryTextBox.Text, out var g) ? g : 0,
                SssDeduction = decimal.TryParse(SSSTextBox.Text, out var s) ? s : 0,
                PhilhealthDeduction = decimal.TryParse(PhilhealthTextBox.Text, out var ph) ? ph : 0,
                PagibigDeduction = decimal.TryParse(PagibigTextBox.Text, out var pi) ? pi : 0,
                OtherDeductions = decimal.TryParse(OtherDeductionsTextBox.Text, out var od) ? od : 0,
                Bonus = decimal.TryParse(BonusTextBox.Text, out var b) ? b : 0,
                NetSalary = decimal.TryParse(NetSalaryTextBox.Text, out var n) ? n : 0,
                BranchName = BranchNameTextBox.Text,
                ShiftType = ShiftTypeTextBox.Text
            };

            bool success;

            if (_isEditMode && _editingPayroll != null)
            {
                payroll.Id = _editingPayroll.Id;
                success = _payrollService.UpdatePayroll(payroll);
                if (success)
                {
                    MessageBox.Show("Payroll updated successfully.");
                }
                else
                {
                    MessageBox.Show("Failed to update payroll.");
                    return;
                }
            }
            else
            {
                success = _payrollService.AddPayroll(payroll);
                if (success)
                {
                    MessageBox.Show("Payroll added successfully.");
                }
                else
                {
                    MessageBox.Show("Failed to add payroll.");
                    return;
                }
            }

            OnPayrollSaved?.Invoke();
            Cancel_Click(sender, e);
        }
    }
}
