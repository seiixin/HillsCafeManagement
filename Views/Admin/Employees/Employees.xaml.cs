using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using HillsCafeManagement.Models;

namespace HillsCafeManagement.Views.Admin.Employees
{
    public partial class Employees : UserControl
    {
        private List<EmployeeModel> _allEmployees;

        public Employees()
        {
            InitializeComponent();
            LoadEmployees();
        }

        private void LoadEmployees()
        {
            try
            {
                _allEmployees = App.DatabaseServices.GetAllEmployees();
                EmployeeDataGrid.ItemsSource = _allEmployees;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load employees.\n\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string query = SearchBox.Text.ToLower();

            var filtered = string.IsNullOrWhiteSpace(query)
                ? _allEmployees
                : _allEmployees.Where(emp =>
                    (!string.IsNullOrEmpty(emp.FullName) && emp.FullName.ToLower().Contains(query)) ||
                    (!string.IsNullOrEmpty(emp.Position) && emp.Position.ToLower().Contains(query)) ||
                    (!string.IsNullOrEmpty(emp.ContactNumber) && emp.ContactNumber.ToLower().Contains(query)) ||
                    (emp.UserAccount != null && emp.UserAccount.Id.ToString().Contains(query))
                ).ToList();

            EmployeeDataGrid.ItemsSource = filtered;
        }

        private void AddEmployee_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("TODO: Add Employee dialog or page.");
        }

        private void EditEmployee_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is EmployeeModel employee)
            {
                MessageBox.Show($"TODO: Edit employee: {employee.FullName}");
            }
        }

        private void DeleteEmployee_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is EmployeeModel employee)
            {
                var result = MessageBox.Show($"Are you sure you want to delete {employee.FullName}?",
                                             "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        // Call delete method from your data service
                        App.DatabaseServices.DeleteEmployee(employee.Id);

                        // Remove from local list and refresh grid
                        _allEmployees.Remove(employee);
                        EmployeeDataGrid.ItemsSource = null;
                        EmployeeDataGrid.ItemsSource = _allEmployees;

                        MessageBox.Show($"Deleted employee: {employee.FullName}");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Failed to delete employee.\n\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

    }
}
