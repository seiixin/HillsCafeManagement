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

        public void AddAttendance(AttendanceModel model)
        {
            using var conn = new MySqlConnection(connectionString);
            conn.Open();

            string sql = "INSERT INTO attendance (employee_id, date, time_in, time_out, status) VALUES (@employeeId, @date, @timeIn, @timeOut, @status)";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@employeeId", model.EmployeeId);
            cmd.Parameters.AddWithValue("@date", model.Date);
            cmd.Parameters.AddWithValue("@timeIn", model.TimeIn);
            cmd.Parameters.AddWithValue("@timeOut", model.TimeOut);
            cmd.Parameters.AddWithValue("@status", model.Status);
            cmd.ExecuteNonQuery();
        }

        public void UpdateAttendance(AttendanceModel model)
        {
            using var conn = new MySqlConnection(connectionString);
            conn.Open();

            string sql = "UPDATE attendance SET employee_id = @employeeId, date = @date, time_in = @timeIn, time_out = @timeOut, status = @status WHERE id = @id";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", model.Id);
            cmd.Parameters.AddWithValue("@employeeId", model.EmployeeId);
            cmd.Parameters.AddWithValue("@date", model.Date);
            cmd.Parameters.AddWithValue("@timeIn", model.TimeIn);
            cmd.Parameters.AddWithValue("@timeOut", model.TimeOut);
            cmd.Parameters.AddWithValue("@status", model.Status);
            cmd.ExecuteNonQuery();
        }

        public void DeleteAttendance(int id)
        {
            using var conn = new MySqlConnection(connectionString);
            conn.Open();

            string sql = "DELETE FROM attendance WHERE id = @id";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }
    }
}
