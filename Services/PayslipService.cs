using HillsCafeManagement.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace HillsCafeManagement.Services
{
    public class PayslipService
    {
        private readonly string _connectionString = "server=localhost;user=root;password=;database=hillscafe_db;";

        // Create payslips from finalized payroll rows for a period
        public int CreatePayslipsFromPayrollPeriod(DateTime periodStart, DateTime periodEnd)
        {
            int created = 0;

            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();
                using var tx = conn.BeginTransaction();

                const string selectPayrollSql = @"
                    SELECT
                        p.employee_id,
                        p.sss_deduction,
                        p.philhealth_deduction,
                        p.pagibig_deduction,
                        p.other_deductions,
                        p.net_salary
                    FROM payroll p
                    WHERE p.start_date = @start AND p.end_date = @end;";

                const string existsPayslipSql = @"
                    SELECT COUNT(*) FROM Payslips
                    WHERE EmployeeId = @empId AND PayDate = @payDate;";

                const string insertPayslipSql = @"
                    INSERT INTO Payslips
                        (EmployeeId, PayDate, HoursWorked, RatePerHour, Deductions, NetSalary, UpdatedDate)
                    VALUES
                        (@empId, @payDate, @hoursWorked, @ratePerHour, @deductions, @netSalary, NOW());";

                using var selectCmd = new MySqlCommand(selectPayrollSql, conn, tx);
                selectCmd.Parameters.AddWithValue("@start", periodStart.Date);
                selectCmd.Parameters.AddWithValue("@end", periodEnd.Date);

                using var reader = selectCmd.ExecuteReader();
                var rows = new List<(int EmpId, decimal Deductions, decimal Net)>();
                while (reader.Read())
                {
                    var empId = reader.GetInt32("employee_id");
                    var sss = reader.IsDBNull(reader.GetOrdinal("sss_deduction")) ? 0m : reader.GetDecimal("sss_deduction");
                    var ph = reader.IsDBNull(reader.GetOrdinal("philhealth_deduction")) ? 0m : reader.GetDecimal("philhealth_deduction");
                    var pi = reader.IsDBNull(reader.GetOrdinal("pagibig_deduction")) ? 0m : reader.GetDecimal("pagibig_deduction");
                    var oth = reader.IsDBNull(reader.GetOrdinal("other_deductions")) ? 0m : reader.GetDecimal("other_deductions");
                    var net = reader.IsDBNull(reader.GetOrdinal("net_salary")) ? 0m : reader.GetDecimal("net_salary");
                    rows.Add((empId, sss + ph + pi + oth, net));
                }
                reader.Close();

                foreach (var row in rows)
                {
                    using var existsCmd = new MySqlCommand(existsPayslipSql, conn, tx);
                    existsCmd.Parameters.AddWithValue("@empId", row.EmpId);
                    existsCmd.Parameters.AddWithValue("@payDate", periodEnd.Date);
                    var exists = Convert.ToInt32(existsCmd.ExecuteScalar()) > 0;
                    if (exists) continue;

                    using var insertCmd = new MySqlCommand(insertPayslipSql, conn, tx);
                    insertCmd.Parameters.AddWithValue("@empId", row.EmpId);
                    insertCmd.Parameters.AddWithValue("@payDate", periodEnd.Date);
                    insertCmd.Parameters.AddWithValue("@hoursWorked", 0m);
                    insertCmd.Parameters.AddWithValue("@ratePerHour", 0m);
                    insertCmd.Parameters.AddWithValue("@deductions", row.Deductions);
                    insertCmd.Parameters.AddWithValue("@netSalary", row.Net);

                    if (insertCmd.ExecuteNonQuery() > 0) created++;
                }

                tx.Commit();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error creating payslips: {ex.Message}");
            }

            return created;
        }

        // Employee payslips (existing)
        public List<PayslipModel> GetEmployeePayslips(int employeeId)
        {
            var list = new List<PayslipModel>();
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();
                using var cmd = new MySqlCommand(@"
                    SELECT PayDate, HoursWorked, RatePerHour, Deductions, NetSalary, UpdatedDate
                    FROM Payslips 
                    WHERE EmployeeId = @EmployeeId
                    ORDER BY PayDate DESC", conn);
                cmd.Parameters.AddWithValue("@EmployeeId", employeeId);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new PayslipModel
                    {
                        PayDate = reader.GetDateTime("PayDate"),
                        HoursWorked = reader.GetDecimal("HoursWorked"),
                        RatePerHour = reader.GetDecimal("RatePerHour"),
                        Deductions = reader.GetDecimal("Deductions"),
                        NetSalary = reader.GetDecimal("NetSalary"),
                        UpdatedDate = reader.IsDBNull("UpdatedDate") ? (DateTime?)null : reader.GetDateTime("UpdatedDate")
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving payslips: {ex.Message}");
            }
            return list;
        }

        // Payslip requests (existing)
        public List<PayslipRequestModel> GetAllPayslipRequests()
        {
            var list = new List<PayslipRequestModel>();
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();
                using var cmd = new MySqlCommand(@"
                    SELECT Id, EmployeeId, FullName, RequestDate, Status, UpdatedDate 
                    FROM PayslipRequests 
                    ORDER BY RequestDate DESC", conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new PayslipRequestModel
                    {
                        Id = reader.GetInt32("Id"),
                        EmployeeId = reader.GetInt32("EmployeeId"),
                        FullName = reader.GetString("FullName"),
                        RequestDate = reader.GetDateTime("RequestDate"),
                        Status = reader.GetString("Status"),
                        UpdatedDate = reader.IsDBNull("UpdatedDate") ? (DateTime?)null : reader.GetDateTime("UpdatedDate")
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving payslip requests: {ex.Message}");
            }
            return list;
        }

        public PayslipRequestModel? GetPayslipRequestById(int id)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();
                using var cmd = new MySqlCommand(@"
                    SELECT Id, EmployeeId, FullName, RequestDate, Status, UpdatedDate 
                    FROM PayslipRequests 
                    WHERE Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", id);
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return new PayslipRequestModel
                    {
                        Id = reader.GetInt32("Id"),
                        EmployeeId = reader.GetInt32("EmployeeId"),
                        FullName = reader.GetString("FullName"),
                        RequestDate = reader.GetDateTime("RequestDate"),
                        Status = reader.GetString("Status"),
                        UpdatedDate = reader.IsDBNull("UpdatedDate") ? (DateTime?)null : reader.GetDateTime("UpdatedDate")
                    };
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving payslip request: {ex.Message}");
            }
            return null;
        }

        public bool CreatePayslipRequest(PayslipRequestModel request)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();
                using var cmd = new MySqlCommand(@"
                    INSERT INTO PayslipRequests (EmployeeId, FullName, RequestDate, Status) 
                    VALUES (@EmployeeId, @FullName, @RequestDate, @Status)", conn);
                cmd.Parameters.AddWithValue("@EmployeeId", request.EmployeeId);
                cmd.Parameters.AddWithValue("@FullName", request.FullName);
                cmd.Parameters.AddWithValue("@RequestDate", request.RequestDate);
                cmd.Parameters.AddWithValue("@Status", request.Status ?? "Pending");

                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error creating payslip request: {ex.Message}");
            }
        }

        public bool UpdateRequestStatus(int id, string newStatus)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();
                using var cmd = new MySqlCommand(@"
                    UPDATE PayslipRequests 
                    SET Status = @Status,
                        UpdatedDate = NOW()
                    WHERE Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Status", newStatus);
                cmd.Parameters.AddWithValue("@Id", id);

                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating request status: {ex.Message}");
            }
        }

        public bool UpdatePayslipRequest(PayslipRequestModel request)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();
                using var cmd = new MySqlCommand(@"
                    UPDATE PayslipRequests 
                    SET EmployeeId = @EmployeeId, 
                        FullName = @FullName, 
                        RequestDate = @RequestDate, 
                        Status = @Status,
                        UpdatedDate = NOW()
                    WHERE Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", request.Id);
                cmd.Parameters.AddWithValue("@EmployeeId", request.EmployeeId);
                cmd.Parameters.AddWithValue("@FullName", request.FullName);
                cmd.Parameters.AddWithValue("@RequestDate", request.RequestDate);
                cmd.Parameters.AddWithValue("@Status", request.Status);

                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating payslip request: {ex.Message}");
            }
        }

        public bool DeletePayslipRequest(int id)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();
                using var cmd = new MySqlCommand("DELETE FROM PayslipRequests WHERE Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", id);

                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting payslip request: {ex.Message}");
            }
        }

        public bool RequestExists(int id)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();
                using var cmd = new MySqlCommand("SELECT COUNT(*) FROM PayslipRequests WHERE Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", id);

                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error checking request existence: {ex.Message}");
            }
        }

        public List<PayslipRequestModel> GetRequestsByStatus(string status)
        {
            var list = new List<PayslipRequestModel>();
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();
                using var cmd = new MySqlCommand(@"
                    SELECT Id, EmployeeId, FullName, RequestDate, Status, UpdatedDate 
                    FROM PayslipRequests 
                    WHERE Status = @Status 
                    ORDER BY RequestDate DESC", conn);
                cmd.Parameters.AddWithValue("@Status", status);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new PayslipRequestModel
                    {
                        Id = reader.GetInt32("Id"),
                        EmployeeId = reader.GetInt32("EmployeeId"),
                        FullName = reader.GetString("FullName"),
                        RequestDate = reader.GetDateTime("RequestDate"),
                        Status = reader.GetString("Status"),
                        UpdatedDate = reader.IsDBNull("UpdatedDate") ? (DateTime?)null : reader.GetDateTime("UpdatedDate")
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving requests by status: {ex.Message}");
            }
            return list;
        }
    }
}
