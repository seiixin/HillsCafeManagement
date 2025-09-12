#nullable enable
using HillsCafeManagement.Models;
using HillsCafeManagement.Services;
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace HillsCafeManagement.Views.Admin.Employees
{
    public partial class AddEditEmployee : UserControl
    {
        private readonly EmployeeService _employeeService = new();
        private readonly PositionSalaryService _positionSalaryService = new();
        private readonly WorkScheduleService _workScheduleService = new();   // NEW

        private readonly bool _isEditMode;
        private readonly EmployeeModel? _editingEmployee;

        // Event to notify parent when saved
        public delegate void EmployeeSavedHandler();
        public event EmployeeSavedHandler? OnEmployeeSaved;

        public AddEditEmployee(EmployeeModel? employee = null)
        {
            InitializeComponent();

            // Populate dropdowns
            PopulatePositions();
            PopulateWorkSchedules(); // NEW

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

                // Position (combo is editable)
                PositionComboBox.Text = employee.Position ?? string.Empty;

                // Salary: keep existing; if empty and manual override is off, try auto-fill from preset
                if (employee.SalaryPerDay.HasValue)
                    SalaryPerDayTextBox.Text = employee.SalaryPerDay.Value.ToString(CultureInfo.InvariantCulture);
                else
                    TryAutoFillSalaryFromPosition();

                ShiftComboBox.SelectedItem = GetComboBoxItemByContent(ShiftComboBox, employee.Shift);

                // NEW: select current Work Schedule (if any)
                if (employee.WorkScheduleId.HasValue)
                    WorkScheduleComboBox.SelectedValue = employee.WorkScheduleId.Value;

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
                SexComboBox.SelectedIndex = 0;
                ShiftComboBox.SelectedIndex = 0;
            }

            // Salary field starts read-only; toggle via manual override
            SetSalaryReadOnlyState();
        }

        // =======================
        // UI Event Handlers
        // =======================

        private void PositionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Auto-fill salary only if manual override is OFF
            if (ManualOverrideCheckBox.IsChecked == true) return;
            TryAutoFillSalaryFromPosition();
        }

        private void ManualOverrideCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            SetSalaryReadOnlyState();
        }

        private void ManualOverrideCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            SetSalaryReadOnlyState();
            // When turning override OFF, refresh to preset
            TryAutoFillSalaryFromPosition();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (Parent is Panel parent)
                parent.Children.Remove(this);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Validate required fields (Full Name and Position)
            var posText = (PositionComboBox.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(FullNameTextBox.Text) || string.IsNullOrWhiteSpace(posText))
            {
                MessageBox.Show("Please fill at least Full Name and Position.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Parse numeric fields safely
            int? age = null;
            if (int.TryParse((AgeTextBox.Text ?? string.Empty).Trim(), out var parsedAge))
                age = parsedAge;

            decimal? salaryPerDay = null;
            if (decimal.TryParse((SalaryPerDayTextBox.Text ?? string.Empty).Trim(),
                                 NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedSalary))
                salaryPerDay = parsedSalary;

            // NEW: get selected WorkScheduleId (SelectedValuePath="Id")
            int? workScheduleId = null;
            if (WorkScheduleComboBox.SelectedValue is int id)
                workScheduleId = id;

            // Prepare employee object
            var employee = new EmployeeModel
            {
                FullName = (FullNameTextBox.Text ?? string.Empty).Trim(),
                Age = age,
                Sex = (SexComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString(),
                Address = (AddressTextBox.Text ?? string.Empty).Trim(),
                Birthday = BirthdayDatePicker.SelectedDate,
                ContactNumber = (ContactNumberTextBox.Text ?? string.Empty).Trim(),
                Position = posText,
                SalaryPerDay = salaryPerDay,
                Shift = (ShiftComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString(),
                WorkScheduleId = workScheduleId, // NEW
                SssNumber = (SssNumberTextBox.Text ?? string.Empty).Trim(),
                PhilhealthNumber = (PhilhealthNumberTextBox.Text ?? string.Empty).Trim(),
                PagibigNumber = (PagibigNumberTextBox.Text ?? string.Empty).Trim(),
                ImageUrl = (ImageUrlTextBox.Text ?? string.Empty).Trim(),
                EmergencyContact = (EmergencyContactTextBox.Text ?? string.Empty).Trim(),
                DateHired = DateHiredDatePicker.SelectedDate
            };

            if (_isEditMode && _editingEmployee != null)
            {
                employee.Id = _editingEmployee.Id;
                employee.CreatedAt = _editingEmployee.CreatedAt;

                var success = _employeeService.UpdateEmployee(employee);
                if (success)
                {
                    MessageBox.Show("Employee updated successfully.", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    OnEmployeeSaved?.Invoke();
                    Cancel_Click(sender, e);
                }
                else
                {
                    MessageBox.Show("Failed to update employee.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                employee.CreatedAt = DateTime.Now;
                var success = _employeeService.AddEmployee(employee);
                if (success)
                {
                    MessageBox.Show("Employee added successfully.", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    OnEmployeeSaved?.Invoke();
                    Cancel_Click(sender, e);
                }
                else
                {
                    MessageBox.Show("Failed to add employee.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // =======================
        // Helpers
        // =======================

        private void PopulatePositions()
        {
            try
            {
                var list = _positionSalaryService.Load()
                                                 .Where(p => p.IsActive)
                                                 .OrderBy(p => p.Position, StringComparer.OrdinalIgnoreCase)
                                                 .Select(p => p.Position)
                                                 .Distinct(StringComparer.OrdinalIgnoreCase)
                                                 .ToList();

                PositionComboBox.ItemsSource = list;
            }
            catch (Exception ex)
            {
                // Non-fatal; just log
                Console.Error.WriteLine("Failed to load position presets: " + ex.Message);
            }
        }

        // NEW: load work schedules into the combo
        private void PopulateWorkSchedules()
        {
            try
            {
                var list = _workScheduleService.Load()
                                               .Where(s => s.IsActive)
                                               .OrderBy(s => s.Label, StringComparer.OrdinalIgnoreCase)
                                               .ToList();

                WorkScheduleComboBox.DisplayMemberPath = "Label";
                WorkScheduleComboBox.SelectedValuePath = "Id";
                WorkScheduleComboBox.ItemsSource = list;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Failed to load work schedules: " + ex.Message);
            }
        }

        private void TryAutoFillSalaryFromPosition()
        {
            var pos = (PositionComboBox.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(pos)) return;
            if (ManualOverrideCheckBox.IsChecked == true) return;

            if (_positionSalaryService.TryGetRate(pos, out var rate))
            {
                SalaryPerDayTextBox.Text = rate.ToString("0.00", CultureInfo.InvariantCulture);
            }
            // else: leave as is if no preset found
        }

        private void SetSalaryReadOnlyState()
        {
            var manual = ManualOverrideCheckBox.IsChecked == true;
            SalaryPerDayTextBox.IsReadOnly = !manual;
            SalaryPerDayTextBox.ToolTip = manual
                ? "Manual override enabled. You can edit this value."
                : "Auto-filled from position. Enable Manual Override to edit.";
        }

        private ComboBoxItem? GetComboBoxItemByContent(ComboBox comboBox, string? content)
        {
            if (string.IsNullOrEmpty(content)) return null;

            foreach (var item in comboBox.Items)
            {
                if (item is ComboBoxItem cbi && string.Equals(cbi.Content?.ToString(), content, StringComparison.Ordinal))
                    return cbi;
            }
            return null;
        }
    }
}
