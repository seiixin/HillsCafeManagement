using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using HillsCafeManagement.Models;

namespace HillsCafeManagement.Services
{
    public class PayslipService
    {
        private readonly string connectionString = "server=localhost;user=root;password=;database=hillscafe_db;";

        public List<PayslipModel> GetEmployeePayslips(int employeeId)
        {
            var list = new List<PayslipModel>();
            using var conn = new MySqlConnection(connectionString);
            conn.Open();
            using var cmd = new MySqlCommand("SELECT PayDate, HoursWorked, RatePerHour, Deductions, NetSalary FROM Payslips WHERE EmployeeId = @EmployeeId", conn);
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
                    NetSalary = reader.GetDecimal("NetSalary")
                });
            }
            return list;
        }

        public List<PayslipRequestModel> GetAllPayslipRequests()
        {
            var list = new List<PayslipRequestModel>();
            using var conn = new MySqlConnection(connectionString);
            conn.Open();
            using var cmd = new MySqlCommand("SELECT EmployeeId, FullName, RequestDate, Status FROM PayslipRequests", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new PayslipRequestModel
                {
                    EmployeeId = reader.GetInt32("EmployeeId"),
                    FullName = reader.GetString("FullName"),
                    RequestDate = reader.GetDateTime("RequestDate"),
                    Status = reader.GetString("Status")
                });
            }
            return list;
        }
    }
}
