// File: ViewModels/PayrollViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MySql.Data.MySqlClient;
using HillsCafeManagement.Models;
using HillsCafeManagement.Helpers;

namespace HillsCafeManagement.ViewModels
{
    public class PayrollViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private readonly string connectionString = "server=localhost;user=root;password=;database=hillscafe_db;";

        public ObservableCollection<PayrollModel> PayrollList { get; set; } = new();

        public ICommand AddPayrollCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand SeeMoreCommand { get; }

        public PayrollViewModel()
        {
            AddPayrollCommand = new RelayCommand(obj => AddPayroll(obj));
            EditCommand = new RelayCommand(obj => EditPayroll(obj));
            DeleteCommand = new RelayCommand(obj => DeletePayroll(obj));
            SeeMoreCommand = new RelayCommand(obj => SeeMore(obj));
            LoadPayroll();
        }

        private void Notify([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public void LoadPayroll()
        {
            PayrollList.Clear();

            using var conn = new MySqlConnection(connectionString);
            conn.Open();

            var query = @"
                SELECT 
                    p.id,
                    e.id AS employee_id,
                    e.full_name,
                    p.start_date,
                    p.end_date,
                    p.total_days_worked,
                    e.salary_per_day,
                    (e.salary_per_day * p.total_days_worked) AS gross_salary,
                    p.sss_deduction + p.philhealth_deduction + p.pagibig_deduction + p.other_deductions AS total_deductions,
                    p.net_salary
                FROM payroll p
                JOIN employees e ON p.employee_id = e.id";

            using var cmd = new MySqlCommand(query, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                PayrollList.Add(new PayrollModel
                {
                    Id = reader.GetInt32("id"),
                    EmployeeId = "E" + reader["employee_id"].ToString()!.PadLeft(3, '0'),
                    FullName = reader["full_name"].ToString() ?? string.Empty,
                    PayDate = Convert.ToDateTime(reader["end_date"]).ToString("MM/dd/yy"),
                    HoursWorked = reader.GetInt32("total_days_worked") * 8,
                    RatePerHour = Convert.ToDecimal(reader["salary_per_day"]) / 8,
                    Deductions = Convert.ToDecimal(reader["total_deductions"]),
                    NetSalary = Convert.ToDecimal(reader["net_salary"])
                });
            }
        }

        public void FilterPayroll(string filter)
        {
            LoadPayroll(); // Reload to reset filters first

            var filtered = PayrollList.Where(p =>
                p.FullName.ToLower().Contains(filter) ||
                p.EmployeeId.ToLower().Contains(filter)
            ).ToList();

            PayrollList.Clear();
            foreach (var item in filtered)
            {
                PayrollList.Add(item);
            }
        }


        private void AddPayroll(object? obj) { /* TODO */ }
        private void EditPayroll(object? obj) { /* TODO */ }
        private void DeletePayroll(object? obj) { /* TODO */ }
        private void SeeMore(object? obj) { /* TODO */ }
    }
}
