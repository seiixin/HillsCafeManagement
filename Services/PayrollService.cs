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
        public bool AddPayroll(PayrollModel payroll)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                const string query = @"
        INSERT INTO payroll (
            employee_id, start_date, end_date, total_days_worked, gross_salary,
            sss_deduction, philhealth_deduction, pagibig_deduction, other_deductions, bonus,
            net_salary, branch_name, shift_type)
        VALUES (
            @EmployeeId, @StartDate, @EndDate, @TotalDaysWorked, @GrossSalary,
            @SssDeduction, @PhilhealthDeduction, @PagibigDeduction, @OtherDeductions, @Bonus,
            @NetSalary, @BranchName, @ShiftType)";

                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@EmployeeId", payroll.EmployeeId);
                cmd.Parameters.AddWithValue("@StartDate", payroll.StartDate);
                cmd.Parameters.AddWithValue("@EndDate", payroll.EndDate);
                cmd.Parameters.AddWithValue("@TotalDaysWorked", payroll.TotalDaysWorked);
                cmd.Parameters.AddWithValue("@GrossSalary", payroll.GrossSalary);
                cmd.Parameters.AddWithValue("@SssDeduction", payroll.SssDeduction);
                cmd.Parameters.AddWithValue("@PhilhealthDeduction", payroll.PhilhealthDeduction);
                cmd.Parameters.AddWithValue("@PagibigDeduction", payroll.PagibigDeduction);
                cmd.Parameters.AddWithValue("@OtherDeductions", payroll.OtherDeductions);
                cmd.Parameters.AddWithValue("@Bonus", payroll.Bonus);
                cmd.Parameters.AddWithValue("@NetSalary", payroll.NetSalary);
                cmd.Parameters.AddWithValue("@BranchName", payroll.BranchName);
                cmd.Parameters.AddWithValue("@ShiftType", payroll.ShiftType);

                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error adding payroll: " + ex.Message);
                return false;
            }
        }

        public bool UpdatePayroll(PayrollModel payroll)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                const string query = @"
        UPDATE payroll SET
            employee_id = @EmployeeId,
            start_date = @StartDate,
            end_date = @EndDate,
            total_days_worked = @TotalDaysWorked,
            gross_salary = @GrossSalary,
            sss_deduction = @SssDeduction,
            philhealth_deduction = @PhilhealthDeduction,
            pagibig_deduction = @PagibigDeduction,
            other_deductions = @OtherDeductions,
            bonus = @Bonus,
            net_salary = @NetSalary,
            branch_name = @BranchName,
            shift_type = @ShiftType
        WHERE id = @Id";

                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", payroll.Id);
                cmd.Parameters.AddWithValue("@EmployeeId", payroll.EmployeeId);
                cmd.Parameters.AddWithValue("@StartDate", payroll.StartDate);
                cmd.Parameters.AddWithValue("@EndDate", payroll.EndDate);
                cmd.Parameters.AddWithValue("@TotalDaysWorked", payroll.TotalDaysWorked);
                cmd.Parameters.AddWithValue("@GrossSalary", payroll.GrossSalary);
                cmd.Parameters.AddWithValue("@SssDeduction", payroll.SssDeduction);
                cmd.Parameters.AddWithValue("@PhilhealthDeduction", payroll.PhilhealthDeduction);
                cmd.Parameters.AddWithValue("@PagibigDeduction", payroll.PagibigDeduction);
                cmd.Parameters.AddWithValue("@OtherDeductions", payroll.OtherDeductions);
                cmd.Parameters.AddWithValue("@Bonus", payroll.Bonus);
                cmd.Parameters.AddWithValue("@NetSalary", payroll.NetSalary);
                cmd.Parameters.AddWithValue("@BranchName", payroll.BranchName);
                cmd.Parameters.AddWithValue("@ShiftType", payroll.ShiftType);

                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error updating payroll: " + ex.Message);
                return false;
            }
        }

    }
}
