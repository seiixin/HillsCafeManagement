using HillsCafeManagement.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace HillsCafeManagement.Services
{
    public class AttendanceService
    {
        private readonly string connectionString = "server=localhost;user=root;password=;database=hillscafe_db;";

        public void ClockIn(int employeeId)
        {
            using var conn = new MySqlConnection(connectionString);
            conn.Open();

            string sql = "INSERT INTO attendance (employee_id, date, time_in, status) VALUES (@id, CURDATE(), CURTIME(), 'Present')";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", employeeId);
            cmd.ExecuteNonQuery();
        }

        public void ClockOut(int employeeId)
        {
            using var conn = new MySqlConnection(connectionString);
            conn.Open();

            string sql = "UPDATE attendance SET time_out = CURTIME() WHERE employee_id = @id AND date = CURDATE()";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", employeeId);
            cmd.ExecuteNonQuery();
        }

        public List<AttendanceModel> GetAttendances(DateTime? date = null, int? employeeId = null)
        {
            var results = new List<AttendanceModel>();
            using var conn = new MySqlConnection(connectionString);
            conn.Open();

            string sql = "SELECT * FROM attendance WHERE 1=1";
            if (date.HasValue) sql += " AND date = @date";
            if (employeeId.HasValue) sql += " AND employee_id = @employeeId";

            using var cmd = new MySqlCommand(sql, conn);
            if (date.HasValue) cmd.Parameters.AddWithValue("@date", date.Value.Date);
            if (employeeId.HasValue) cmd.Parameters.AddWithValue("@employeeId", employeeId.Value);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                results.Add(new AttendanceModel
                {
                    Id = reader.GetInt32("id"),
                    EmployeeId = reader.GetInt32("employee_id"),
                    Date = reader.GetDateTime("date"),
                    TimeIn = reader.IsDBNull("time_in") ? null : reader.GetTimeSpan("time_in"),
                    TimeOut = reader.IsDBNull("time_out") ? null : reader.GetTimeSpan("time_out"),
                    Status = reader.GetString("status")
                });
            }
            return results;
        }
    }
}