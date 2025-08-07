using HillsCafeManagement.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace HillsCafeManagement.Services
{
    public class PayslipService
    {
        private readonly string connectionString = "server=localhost;user=root;password=;database=hillscafe_db;";

        // READ: Get employee payslips
        public List<PayslipModel> GetEmployeePayslips(int employeeId)
        {
            var list = new List<PayslipModel>();
            try
            {
                using var conn = new MySqlConnection(connectionString);
                conn.Open();
                using var cmd = new MySqlCommand(@"SELECT PayDate, HoursWorked, RatePerHour, Deductions, NetSalary, UpdatedDate
                                                   FROM Payslips 
                                                   WHERE EmployeeId = @EmployeeId", conn);
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
                        UpdatedDate = reader.IsDBNull("UpdatedDate") ? null : reader.GetDateTime("UpdatedDate")
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving payslips: {ex.Message}");
            }
            return list;
        }

        // READ: Get all payslip requests
        public List<PayslipRequestModel> GetAllPayslipRequests()
        {
            var list = new List<PayslipRequestModel>();
            try
            {
                using var conn = new MySqlConnection(connectionString);
                conn.Open();
                using var cmd = new MySqlCommand(@"SELECT Id, EmployeeId, FullName, RequestDate, Status, UpdatedDate 
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
                        UpdatedDate = reader.IsDBNull("UpdatedDate") ? null : reader.GetDateTime("UpdatedDate")
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving payslip requests: {ex.Message}");
            }
            return list;
        }

        // READ: Get single payslip request by ID
        public PayslipRequestModel GetPayslipRequestById(int id)
        {
            try
            {
                using var conn = new MySqlConnection(connectionString);
                conn.Open();
                using var cmd = new MySqlCommand(@"SELECT Id, EmployeeId, FullName, RequestDate, Status, UpdatedDate 
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
                        UpdatedDate = reader.IsDBNull("UpdatedDate") ? null : reader.GetDateTime("UpdatedDate")
                    };
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving payslip request: {ex.Message}");
            }
            return null;
        }

        // CREATE: Add new payslip request
        public bool CreatePayslipRequest(PayslipRequestModel request)
        {
            try
            {
                using var conn = new MySqlConnection(connectionString);
                conn.Open();
                using var cmd = new MySqlCommand(@"INSERT INTO PayslipRequests (EmployeeId, FullName, RequestDate, Status) 
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

        // UPDATE: Update request status only
        public bool UpdateRequestStatus(int id, string newStatus)
        {
            try
            {
                using var conn = new MySqlConnection(connectionString);
                conn.Open();
                using var cmd = new MySqlCommand(@"UPDATE PayslipRequests 
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

        // UPDATE: Update full request
        public bool UpdatePayslipRequest(PayslipRequestModel request)
        {
            try
            {
                using var conn = new MySqlConnection(connectionString);
                conn.Open();
                using var cmd = new MySqlCommand(@"UPDATE PayslipRequests 
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

        // DELETE: Delete request
        public bool DeletePayslipRequest(int id)
        {
            try
            {
                using var conn = new MySqlConnection(connectionString);
                conn.Open();
                using var cmd = new MySqlCommand(@"DELETE FROM PayslipRequests WHERE Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", id);

                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting payslip request: {ex.Message}");
            }
        }

        // EXISTS: Check if request exists
        public bool RequestExists(int id)
        {
            try
            {
                using var conn = new MySqlConnection(connectionString);
                conn.Open();
                using var cmd = new MySqlCommand(@"SELECT COUNT(*) FROM PayslipRequests WHERE Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", id);

                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error checking request existence: {ex.Message}");
            }
        }

        // FILTER: Get requests by status
        public List<PayslipRequestModel> GetRequestsByStatus(string status)
        {
            var list = new List<PayslipRequestModel>();
            try
            {
                using var conn = new MySqlConnection(connectionString);
                conn.Open();
                using var cmd = new MySqlCommand(@"SELECT Id, EmployeeId, FullName, RequestDate, Status, UpdatedDate 
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
                        UpdatedDate = reader.IsDBNull("UpdatedDate") ? null : reader.GetDateTime("UpdatedDate")
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
