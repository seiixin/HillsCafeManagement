using HillsCafeManagement.Models;
using HillsCafeManagement.Services;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace HillsCafeManagement.Views.Admin.Employees
{
    public partial class AddEditEmployee : UserControl
    {
        private readonly EmployeeService _employeeService = new();
        private readonly bool _isEditMode;
        private readonly EmployeeModel? _editingEmployee;

        // Event to notify parent when saved
        public delegate void EmployeeSavedHandler();
        public event EmployeeSavedHandler? OnEmployeeSaved;

        public AddEditEmployee(EmployeeModel? employee = null)
        {
            InitializeComponent();

            if (employee != null)
            {
                _isEditMode = true;
                _editingEmployee = employee;

                TitleText.Text = "Edit Employee";

                // Populate fields
                FullNameTextBox.Text = employee.FullName ?? string.Empty;
                AgeTextBox.Text = employee.Age?.ToString() ?? string.Empty;
                SexComboBox.SelectedItem = GetComboBoxItemByContent(SexComboBox, employee.Sex);
                AddressTextBox.Text = employee.Address ?? string.Empty;
                BirthdayDatePicker.SelectedDate = employee.Birthday;
                ContactNumberTextBox.Text = employee.ContactNumber ?? string.Empty;
                PositionTextBox.Text = employee.Position ?? string.Empty;
                SalaryPerDayTextBox.Text = employee.SalaryPerDay?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
                ShiftComboBox.SelectedItem = GetComboBoxItemByContent(ShiftComboBox, employee.Shift);
                SssNumberTextBox.Text = employee.SssNumber ?? string.Empty;
                PhilhealthNumberTextBox.Text = employee.PhilhealthNumber ?? string.Empty;
                PagibigNumberTextBox.Text = employee.PagibigNumber ?? string.Empty;
                ImageUrlTextBox.Text = employee.ImageUrl ?? string.Empty;
                EmergencyContactTextBox.Text = employee.EmergencyContact ?? string.Empty;
                DateHiredDatePicker.SelectedDate = employee.DateHired;
            }
            else
            {
                _isEditMode = false;
                TitleText.Text = "Add New Employee";

                // Defaults (optional)
                SexComboBox.SelectedIndex = 0;
                ShiftComboBox.SelectedIndex = 0;
            }
        }

        private ComboBoxItem? GetComboBoxItemByContent(ComboBox comboBox, string? content)
        {
            if (string.IsNullOrEmpty(content)) return null;

            foreach (var item in comboBox.Items)
            {
                if (item is ComboBoxItem cbi && cbi.Content.ToString() == content)
                    return cbi;
            }
            return null;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            var parent = this.Parent as Panel;
            parent?.Children.Remove(this);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Validate required fields (example: FullName and Position)
            if (string.IsNullOrWhiteSpace(FullNameTextBox.Text) || string.IsNullOrWhiteSpace(PositionTextBox.Text))
            {
                MessageBox.Show("Please fill at least Full Name and Position.");
                return;
            }

            // Parse numeric fields safely
            int? age = null;
            if (int.TryParse(AgeTextBox.Text.Trim(), out int parsedAge))
                age = parsedAge;

            decimal? salaryPerDay = null;
            if (decimal.TryParse(SalaryPerDayTextBox.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsedSalary))
                salaryPerDay = parsedSalary;

            // Prepare employee object
            var employee = new EmployeeModel
            {
                FullName = FullNameTextBox.Text.Trim(),
                Age = age,
                Sex = (SexComboBox.SelectedItem as ComboBoxItem)?.Content.ToString(),
                Address = AddressTextBox.Text.Trim(),
                Birthday = BirthdayDatePicker.SelectedDate,
                ContactNumber = ContactNumberTextBox.Text.Trim(),
                Position = PositionTextBox.Text.Trim(),
                SalaryPerDay = salaryPerDay,
                Shift = (ShiftComboBox.SelectedItem as ComboBoxItem)?.Content.ToString(),
                SssNumber = SssNumberTextBox.Text.Trim(),
                PhilhealthNumber = PhilhealthNumberTextBox.Text.Trim(),
                PagibigNumber = PagibigNumberTextBox.Text.Trim(),
                ImageUrl = ImageUrlTextBox.Text.Trim(),
                EmergencyContact = EmergencyContactTextBox.Text.Trim(),
                DateHired = DateHiredDatePicker.SelectedDate
            };

            if (_isEditMode && _editingEmployee != null)
            {
                employee.Id = _editingEmployee.Id;
                employee.CreatedAt = _editingEmployee.CreatedAt;

                bool success = _employeeService.UpdateEmployee(employee);

                if (success)
                {
                    MessageBox.Show("Employee updated successfully.");
                    OnEmployeeSaved?.Invoke();
                    Cancel_Click(sender, e);
                }
                else
                {
                    MessageBox.Show("Failed to update employee.");
                }
            }
            else
            {
                employee.CreatedAt = DateTime.Now;

                bool success = _employeeService.AddEmployee(employee);

                if (success)
                {
                    MessageBox.Show("Employee added successfully.");
                    OnEmployeeSaved?.Invoke();
                    Cancel_Click(sender, e);
                }
                else
                {
                    MessageBox.Show("Failed to add employee.");
                }
            }
        }
    }
}
