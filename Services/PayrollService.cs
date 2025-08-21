using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using HillsCafeManagement.Models;

namespace HillsCafeManagement.Services
{
    public class PayrollService
    {
        private readonly string _connectionString =
            "server=localhost;user=root;password=;database=hillscafe_db;";

        // ---------- Period helpers ----------
        public (DateTime start, DateTime end) GetFirstHalfRange(int year, int month)
            => (new DateTime(year, month, 1, 0, 0, 0),
                new DateTime(year, month, 15, 23, 59, 59));

        public (DateTime start, DateTime end) GetSecondHalfRange(int year, int month)
            => (new DateTime(year, month, 16, 0, 0, 0),
                new DateTime(year, month, DateTime.DaysInMonth(year, month), 23, 59, 59));

        // ---------- Generate for period (matches your current schema) ----------
        // Assumptions:
        //  - employees: has at least (id, full_name). No branch/shift/rates required.
        //  - attendance: (id, employee_id, date, time_in, time_out, status ['Present',...])
        //  - No leave_requests table (paid/unpaid leave = 0 for now)
        //  - If employee has no configured rate, we fallback to last payroll’s per-day rate (if any); otherwise 0.
        public List<PayrollModel> GenerateForPeriod(DateTime periodStart, DateTime periodEnd)
        {
            var results = new List<PayrollModel>();

            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                // Keep this minimal to avoid “unknown column” errors.
                const string getEmployeesSql = @"
                    SELECT e.id, e.full_name
                    FROM employees e";

                var employees = new List<(int Id, string Name)>();
                using (var empCmd = new MySqlCommand(getEmployeesSql, conn))
                using (var r = empCmd.ExecuteReader())
                {
                    while (r.Read())
                        employees.Add((r.GetInt32("id"), r["full_name"]?.ToString() ?? ""));
                }

                // Days worked = distinct attendance dates marked Present in the window
                const string attendanceDaysSql = @"
                    SELECT COUNT(DISTINCT a.date) 
                    FROM attendance a
                    WHERE a.employee_id = @empId
                      AND a.date BETWEEN @start AND @end
                      AND (a.status = 'Present' OR a.status IS NULL)";

                // Hours = sum of (time_out - time_in) in hours (if you need hourly payroll)
                const string attendanceHoursSql = @"
                    SELECT IFNULL(SUM(TIME_TO_SEC(TIMEDIFF(a.time_out, a.time_in))/3600), 0)
                    FROM attendance a
                    WHERE a.employee_id = @empId
                      AND a.date BETWEEN @start AND @end
                      AND a.time_in IS NOT NULL
                      AND a.time_out IS NOT NULL";

                foreach (var emp in employees)
                {
                    int daysWorked = ExecuteScalarInt(conn, attendanceDaysSql, emp.Id, periodStart, periodEnd);
                    decimal hoursWorked = ExecuteScalarDecimal(conn, attendanceHoursSql, emp.Id, periodStart, periodEnd);

                    // No leave table yet
                    int paidLeaveDays = 0;
                    int unpaidLeaveDays = 0;

                    // Figure out a per-day rate:
                    // 1) try last payroll’s gross/total_days_worked
                    // 2) else 0 (admin can edit later)
                    decimal ratePerDay = GetLastPerDayRate(conn, emp.Id);

                    decimal gross = ratePerDay * (daysWorked + paidLeaveDays);
                    decimal unpaidLeaveDeduction = ratePerDay * unpaidLeaveDays;

                    // Statutory placeholders (edit later if you have rules)
                    decimal sss = 0m, philhealth = 0m, pagibig = 0m, bonus = 0m;
                    decimal otherDeductions = unpaidLeaveDeduction;
                    decimal net = Math.Max(0m, gross - (sss + philhealth + pagibig + otherDeductions) + bonus);

                    results.Add(new PayrollModel
                    {
                        EmployeeId = emp.Id,
                        EmployeeFullName = emp.Name,
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
                        BranchName = null, // you can fill from another table later
                        ShiftType = null
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
                    cmd.Parameters.AddWithValue("@BranchName",
                        string.IsNullOrWhiteSpace(p.BranchName) ? (object)DBNull.Value : p.BranchName);
                    cmd.Parameters.AddWithValue("@ShiftType",
                        string.IsNullOrWhiteSpace(p.ShiftType) ? (object)DBNull.Value : p.ShiftType);
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

        // ---------- Existing CRUD (unchanged) ----------
        public List<PayrollModel> GetAllPayrolls()
        {
            var list = new List<PayrollModel>();
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();
                const string sql = @"
                    SELECT p.id, p.employee_id, p.start_date, p.end_date, p.total_days_worked, p.gross_salary,
                           p.sss_deduction, p.philhealth_deduction, p.pagibig_deduction, p.other_deductions, p.bonus,
                           p.net_salary, p.branch_name, p.shift_type,
                           e.full_name
                    FROM payroll p
                    LEFT JOIN employees e ON p.employee_id = e.id
                    ORDER BY p.id DESC";
                using var cmd = new MySqlCommand(sql, conn);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    list.Add(new PayrollModel
                    {
                        Id = r.GetInt32("id"),
                        EmployeeId = r.GetInt32("employee_id"),
                        StartDate = r.GetDateTime("start_date"),
                        EndDate = r.GetDateTime("end_date"),
                        TotalDaysWorked = r.GetInt32("total_days_worked"),
                        GrossSalary = r.GetDecimal("gross_salary"),
                        SssDeduction = r.GetDecimal("sss_deduction"),
                        PhilhealthDeduction = r.GetDecimal("philhealth_deduction"),
                        PagibigDeduction = r.GetDecimal("pagibig_deduction"),
                        OtherDeductions = r.GetDecimal("other_deductions"),
                        Bonus = r.GetDecimal("bonus"),
                        NetSalary = r.GetDecimal("net_salary"),
                        BranchName = r["branch_name"]?.ToString(),
                        ShiftType = r["shift_type"]?.ToString(),
                        EmployeeFullName = r["full_name"]?.ToString() ?? ""
                    });
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error fetching payrolls: " + ex.Message);
            }
            return list;
        }

        public bool DeletePayrollById(int id)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();
                using var cmd = new MySqlCommand("DELETE FROM payroll WHERE id=@Id", conn);
                cmd.Parameters.AddWithValue("@Id", id);
                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error deleting payroll: " + ex.Message);
                return false;
            }
        }

        public bool AddPayroll(PayrollModel p) => SaveGeneratedPayrolls(new[] { p });

        public bool UpdatePayroll(PayrollModel p)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();
                const string sql = @"
                    UPDATE payroll SET
                        employee_id=@EmployeeId, start_date=@StartDate, end_date=@EndDate,
                        total_days_worked=@TotalDaysWorked, gross_salary=@GrossSalary,
                        sss_deduction=@SssDeduction, philhealth_deduction=@PhilhealthDeduction,
                        pagibig_deduction=@PagibigDeduction, other_deductions=@OtherDeductions, bonus=@Bonus,
                        net_salary=@NetSalary, branch_name=@BranchName, shift_type=@ShiftType
                    WHERE id=@Id";
                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Id", p.Id);
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

        private static decimal GetLastPerDayRate(MySqlConnection conn, int empId)
        {
            const string sql = @"
                SELECT ROUND(IFNULL(gross_salary / NULLIF(total_days_worked,0), 0), 2) AS rate
                FROM payroll
                WHERE employee_id = @empId
                ORDER BY end_date DESC, id DESC
                LIMIT 1";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@empId", empId);
            var obj = cmd.ExecuteScalar();
            return (obj == null || obj == DBNull.Value) ? 0m : Convert.ToDecimal(obj);
        }
    }
}
