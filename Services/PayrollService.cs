using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using HillsCafeManagement.Models;

namespace HillsCafeManagement.Services
{
    public class PayrollService
    {
        private readonly string _connectionString = "server=localhost;user=root;password=;database=hillscafe_db;";

        // READ all payrolls
        public List<PayrollModel> GetAllPayrolls()
        {
            var payrolls = new List<PayrollModel>();
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                const string query = @"
            SELECT 
                p.id, p.employee_id, p.start_date, p.end_date, p.total_days_worked, p.gross_salary,
                p.sss_deduction, p.philhealth_deduction, p.pagibig_deduction, p.other_deductions, p.bonus,
                p.net_salary, p.branch_name, p.shift_type,
                e.full_name
            FROM payroll p
            LEFT JOIN employees e ON p.employee_id = e.id
            ORDER BY p.id DESC";

                using var cmd = new MySqlCommand(query, conn);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    payrolls.Add(new PayrollModel
                    {
                        Id = reader.GetInt32("id"),
                        EmployeeId = reader.GetInt32("employee_id"),
                        StartDate = reader.GetDateTime("start_date"),
                        EndDate = reader.GetDateTime("end_date"),
                        TotalDaysWorked = reader.GetInt32("total_days_worked"),
                        GrossSalary = reader.GetDecimal("gross_salary"),
                        SssDeduction = reader.GetDecimal("sss_deduction"),
                        PhilhealthDeduction = reader.GetDecimal("philhealth_deduction"),
                        PagibigDeduction = reader.GetDecimal("pagibig_deduction"),
                        OtherDeductions = reader.GetDecimal("other_deductions"),
                        Bonus = reader.GetDecimal("bonus"),
                        NetSalary = reader.GetDecimal("net_salary"),
                        BranchName = reader["branch_name"].ToString(),
                        ShiftType = reader["shift_type"].ToString(),
                        EmployeeFullName = reader["full_name"]?.ToString() ?? ""  // <-- map here
                    });
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error fetching payrolls: " + ex.Message);
            }

            return payrolls;
        }

        // DELETE payroll by id
        public bool DeletePayrollById(int id)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                const string query = "DELETE FROM payroll WHERE id = @Id";

                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);

                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error deleting payroll: " + ex.Message);
                return false;
            }
        }
    }
}
