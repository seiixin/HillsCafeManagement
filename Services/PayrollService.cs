using System;
using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;
using HillsCafeManagement.Models;

namespace HillsCafeManagement.Services
{
    public class PayrollService
    {
        private readonly string _connectionString = "server=localhost;user=root;password=;database=hillscafe_db;";

        // ---------- Period helpers ----------
        public (DateTime start, DateTime end) GetFirstHalfRange(int year, int month)
        {
            var start = new DateTime(year, month, 1, 0, 0, 0);
            var end = new DateTime(year, month, 15, 23, 59, 59);
            return (start, end);
        }

        public (DateTime start, DateTime end) GetSecondHalfRange(int year, int month)
        {
            var start = new DateTime(year, month, 16, 0, 0, 0);
            var end = new DateTime(year, month, DateTime.DaysInMonth(year, month), 23, 59, 59);
            return (start, end);
        }

        // ---------- Generate for period ----------
        public List<PayrollModel> GenerateForPeriod(DateTime periodStart, DateTime periodEnd)
        {
            var results = new List<PayrollModel>();

            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                // Load employees with rates (nullable)
                const string getEmployeesSql = @"
                    SELECT 
                        e.id,
                        e.full_name,
                        COALESCE(e.branch_name, '') AS branch_name,
                        COALESCE(e.shift_type, '') AS shift_type,
                        e.rate_per_day,
                        e.rate_per_hour
                    FROM employees e
                    WHERE e.is_active = 1 OR e.is_active IS NULL;";

                var employees = new List<(int Id, string Name, string Branch, string Shift, decimal? RatePerDay, decimal? RatePerHour)>();
                using (var empCmd = new MySqlCommand(getEmployeesSql, conn))
                using (var empReader = empCmd.ExecuteReader())
                {
                    while (empReader.Read())
                    {
                        employees.Add((
                            empReader.GetInt32("id"),
                            empReader["full_name"]?.ToString() ?? "",
                            empReader["branch_name"]?.ToString() ?? "",
                            empReader["shift_type"]?.ToString() ?? "",
                            empReader.IsDBNull(empReader.GetOrdinal("rate_per_day")) ? (decimal?)null : empReader.GetDecimal("rate_per_day"),
                            empReader.IsDBNull(empReader.GetOrdinal("rate_per_hour")) ? (decimal?)null : empReader.GetDecimal("rate_per_hour")
                        ));
                    }
                }

                const string attendanceDaysSql = @"
                    SELECT COUNT(DISTINCT work_date) AS days_worked
                    FROM attendance
                    WHERE employee_id = @empId
                      AND work_date BETWEEN @start AND @end
                      AND (is_present = 1 OR is_present IS NULL);";

                const string attendanceHoursSql = @"
                    SELECT IFNULL(SUM(hours_worked), 0) AS hours_worked
                    FROM attendance
                    WHERE employee_id = @empId
                      AND work_date BETWEEN @start AND @end;";

                const string unpaidLeaveDaysSql = @"
                    SELECT IFNULL(SUM(
                        DATEDIFF(LEAST(@end, lr.end_date), GREATEST(@start, lr.start_date)) + 1
                    ), 0) AS days
                    FROM leave_requests lr
                    WHERE lr.employee_id = @empId
                      AND lr.status = 'Approved'
                      AND lr.is_paid = 0
                      AND lr.start_date <= @end
                      AND lr.end_date >= @start;";

                const string paidLeaveDaysSql = @"
                    SELECT IFNULL(SUM(
                        DATEDIFF(LEAST(@end, lr.end_date), GREATEST(@start, lr.start_date)) + 1
                    ), 0) AS days
                    FROM leave_requests lr
                    WHERE lr.employee_id = @empId
                      AND lr.status = 'Approved'
                      AND lr.is_paid = 1
                      AND lr.start_date <= @end
                      AND lr.end_date >= @start;";

                foreach (var emp in employees)
                {
                    int daysWorked = ExecuteScalarInt(conn, attendanceDaysSql, emp.Id, periodStart, periodEnd);
                    decimal hoursWorked = ExecuteScalarDecimal(conn, attendanceHoursSql, emp.Id, periodStart, periodEnd);
                    int unpaidLeaveDays = ExecuteScalarInt(conn, unpaidLeaveDaysSql, emp.Id, periodStart, periodEnd);
                    int paidLeaveDays = ExecuteScalarInt(conn, paidLeaveDaysSql, emp.Id, periodStart, periodEnd);

                    decimal gross = 0m;
                    decimal unpaidLeaveDeduction = 0m;

                    if (emp.RatePerDay.HasValue)
                    {
                        gross = (daysWorked + paidLeaveDays) * emp.RatePerDay.Value;
                        unpaidLeaveDeduction = unpaidLeaveDays * emp.RatePerDay.Value;
                    }
                    else if (emp.RatePerHour.HasValue)
                    {
                        gross = hoursWorked * emp.RatePerHour.Value;
                        // if hourly and you want unpaid-leave deductions, define scheduled hours and multiply here
                        unpaidLeaveDeduction = 0m;
                    }

                    decimal sss = 0m;
                    decimal philhealth = 0m;
                    decimal pagibig = 0m;
                    decimal otherDeductions = unpaidLeaveDeduction;
                    decimal bonus = 0m;

                    decimal net = Math.Max(0m, gross - (sss + philhealth + pagibig + otherDeductions) + bonus);

                    results.Add(new PayrollModel
                    {
                        EmployeeId = emp.Id,
                        StartDate = periodStart.Date,
                        EndDate = periodEnd.Date,
                        TotalDaysWorked = daysWorked,
                        GrossSalary = Math.Round(gross, 2),
                        SssDeduction = Math.Round(sss, 2),
                        PhilhealthDeduction = Math.Round(philhealth, 2),
                        PagibigDeduction = Math.Round(pagibig, 2),
                        OtherDeductions = Math.Round(otherDeductions, 2),
                        Bonus = Math.Round(bonus, 2),
                        NetSalary = Math.Round(net, 2),
                        BranchName = emp.Branch,
                        ShiftType = emp.Shift,
                        EmployeeFullName = emp.Name
                    });
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error generating payrolls: " + ex.Message);
            }

            return results;
        }

        // ---------- Batch save ----------
        public bool SaveGeneratedPayrolls(IEnumerable<PayrollModel> payrolls)
        {
            if (payrolls == null) return false;

            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();
                using var tx = conn.BeginTransaction();

                const string insertSql = @"
                    INSERT INTO payroll (
                        employee_id, start_date, end_date, total_days_worked, gross_salary,
                        sss_deduction, philhealth_deduction, pagibig_deduction, other_deductions, bonus,
                        net_salary, branch_name, shift_type
                    )
                    VALUES (
                        @EmployeeId, @StartDate, @EndDate, @TotalDaysWorked, @GrossSalary,
                        @SssDeduction, @PhilhealthDeduction, @PagibigDeduction, @OtherDeductions, @Bonus,
                        @NetSalary, @BranchName, @ShiftType
                    );";

                foreach (var p in payrolls)
                {
                    using var cmd = new MySqlCommand(insertSql, conn, tx);
                    cmd.Parameters.AddWithValue("@EmployeeId", p.EmployeeId);
                    cmd.Parameters.AddWithValue("@StartDate", p.StartDate);
                    cmd.Parameters.AddWithValue("@EndDate", p.EndDate);
                    cmd.Parameters.AddWithValue("@TotalDaysWorked", p.TotalDaysWorked);
                    cmd.Parameters.AddWithValue("@GrossSalary", p.GrossSalary);
                    cmd.Parameters.AddWithValue("@SssDeduction", p.SssDeduction);
                    cmd.Parameters.AddWithValue("@PhilhealthDeduction", p.PhilhealthDeduction);
                    cmd.Parameters.AddWithValue("@PagibigDeduction", p.PagibigDeduction);
                    cmd.Parameters.AddWithValue("@OtherDeductions", p.OtherDeductions);
                    cmd.Parameters.AddWithValue("@Bonus", p.Bonus);
                    cmd.Parameters.AddWithValue("@NetSalary", p.NetSalary);
                    cmd.Parameters.AddWithValue("@BranchName", string.IsNullOrWhiteSpace(p.BranchName) ? (object)DBNull.Value : p.BranchName);
                    cmd.Parameters.AddWithValue("@ShiftType", string.IsNullOrWhiteSpace(p.ShiftType) ? (object)DBNull.Value : p.ShiftType);
                    cmd.ExecuteNonQuery();
                }

                tx.Commit();
                return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error saving generated payrolls: " + ex.Message);
                return false;
            }
        }

        // ---------- Existing CRUD ----------
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
                        BranchName = reader["branch_name"]?.ToString(),
                        ShiftType = reader["shift_type"]?.ToString(),
                        EmployeeFullName = reader["full_name"]?.ToString() ?? ""
                    });
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error fetching payrolls: " + ex.Message);
            }

            return payrolls;
        }

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
                cmd.Parameters.AddWithValue("@BranchName", payroll.BranchName ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@ShiftType", payroll.ShiftType ?? (object)DBNull.Value);

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
                cmd.Parameters.AddWithValue("@BranchName", payroll.BranchName ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@ShiftType", payroll.ShiftType ?? (object)DBNull.Value);

                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error updating payroll: " + ex.Message);
                return false;
            }
        }

        // ---------- helpers ----------
        private static int ExecuteScalarInt(MySqlConnection conn, string sql, int empId, DateTime start, DateTime end)
        {
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@empId", empId);
            cmd.Parameters.AddWithValue("@start", start);
            cmd.Parameters.AddWithValue("@end", end);
            var obj = cmd.ExecuteScalar();
            return (obj == null || obj == DBNull.Value) ? 0 : Convert.ToInt32(obj);
        }

        private static decimal ExecuteScalarDecimal(MySqlConnection conn, string sql, int empId, DateTime start, DateTime end)
        {
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@empId", empId);
            cmd.Parameters.AddWithValue("@start", start);
            cmd.Parameters.AddWithValue("@end", end);
            var obj = cmd.ExecuteScalar();
            return (obj == null || obj == DBNull.Value) ? 0m : Convert.ToDecimal(obj);
        }
    }
}
