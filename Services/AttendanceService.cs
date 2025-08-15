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

        // =========================
        // Auto-NOW versions (no timestamp passed)
        // =========================

        /// <summary>
        /// Always inserts a new attendance row with current date/time as time_in.
        /// Multiple entries per day are allowed.
        /// </summary>
        public void ClockIn(int employeeId)
        {
            using var conn = new MySqlConnection(connectionString);
            conn.Open();

            const string sql = @"
                INSERT INTO attendance (employee_id, date, time_in, status)
                VALUES (@id, CURDATE(), CURTIME(), 'Present');";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", employeeId);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Sets time_out=NOW for the most recent open record (same date, no time_out yet).
        /// If there is no open record for today, this is a no-op.
        /// </summary>
        public void ClockOut(int employeeId)
        {
            using var conn = new MySqlConnection(connectionString);
            conn.Open();

            const string sql = @"
                UPDATE attendance a
                JOIN (
                    SELECT id
                    FROM attendance
                    WHERE employee_id = @id
                      AND date = CURDATE()
                      AND time_out IS NULL
                    ORDER BY time_in DESC
                    LIMIT 1
                ) t ON a.id = t.id
                SET a.time_out = CURTIME();";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", employeeId);
            cmd.ExecuteNonQuery();
        }

        // =========================
        // Manual timestamp overloads
        // =========================

        /// <summary>
        /// Inserts a new row using the provided timestamp (PH local assumed by caller).
        /// </summary>
        public void ClockIn(int employeeId, DateTime timestamp)
        {
            using var conn = new MySqlConnection(connectionString);
            conn.Open();

            const string sql = @"
                INSERT INTO attendance (employee_id, date, time_in, status)
                VALUES (@id, @date, @timeIn, 'Present');";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", employeeId);
            cmd.Parameters.Add("@date", MySqlDbType.Date).Value = timestamp.Date;
            cmd.Parameters.Add("@timeIn", MySqlDbType.Time).Value = timestamp.TimeOfDay;
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Sets time_out for the most recent open record on the same DATE as the timestamp.
        /// If none found, no-op (does not insert).
        /// </summary>
        public void ClockOut(int employeeId, DateTime timestamp)
        {
            using var conn = new MySqlConnection(connectionString);
            conn.Open();

            const string sql = @"
                UPDATE attendance a
                JOIN (
                    SELECT id
                    FROM attendance
                    WHERE employee_id = @id
                      AND date = @date
                      AND time_out IS NULL
                    ORDER BY time_in DESC
                    LIMIT 1
                ) t ON a.id = t.id
                SET a.time_out = @timeOut;";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", employeeId);
            cmd.Parameters.Add("@date", MySqlDbType.Date).Value = timestamp.Date;
            cmd.Parameters.Add("@timeOut", MySqlDbType.Time).Value = timestamp.TimeOfDay;
            cmd.ExecuteNonQuery();
        }

        // =========================
        // Queries & Admin ops
        // =========================

        public List<AttendanceModel> GetAttendances(DateTime? date = null, int? employeeId = null)
        {
            var results = new List<AttendanceModel>();
            using var conn = new MySqlConnection(connectionString);
            conn.Open();

            var sql = "SELECT * FROM attendance WHERE 1=1";
            if (date.HasValue) sql += " AND date = @date";
            if (employeeId.HasValue) sql += " AND employee_id = @employeeId";
            sql += " ORDER BY date DESC, time_in DESC, id DESC";

            using var cmd = new MySqlCommand(sql, conn);
            if (date.HasValue)
                cmd.Parameters.Add("@date", MySqlDbType.Date).Value = date.Value.Date;
            if (employeeId.HasValue)
                cmd.Parameters.AddWithValue("@employeeId", employeeId.Value);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                results.Add(new AttendanceModel
                {
                    Id = reader.GetInt32("id"),
                    EmployeeId = reader.GetInt32("employee_id"),
                    Date = reader.GetDateTime("date"),
                    TimeIn = reader.IsDBNull(reader.GetOrdinal("time_in")) ? (TimeSpan?)null : reader.GetTimeSpan("time_in"),
                    TimeOut = reader.IsDBNull(reader.GetOrdinal("time_out")) ? (TimeSpan?)null : reader.GetTimeSpan("time_out"),
                    Status = reader.GetString("status")
                });
            }
            return results;
        }

        public void AddAttendance(AttendanceModel model)
        {
            using var conn = new MySqlConnection(connectionString);
            conn.Open();

            const string sql = @"
                INSERT INTO attendance (employee_id, date, time_in, time_out, status)
                VALUES (@employeeId, @date, @timeIn, @timeOut, @status);";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@employeeId", model.EmployeeId);
            cmd.Parameters.Add("@date", MySqlDbType.Date).Value = model.Date.Date;
            cmd.Parameters.Add("@timeIn", MySqlDbType.Time).Value = (object?)model.TimeIn ?? DBNull.Value;
            cmd.Parameters.Add("@timeOut", MySqlDbType.Time).Value = (object?)model.TimeOut ?? DBNull.Value;
            cmd.Parameters.AddWithValue("@status", model.Status ?? "Present");
            cmd.ExecuteNonQuery();
        }

        public void UpdateAttendance(AttendanceModel model)
        {
            using var conn = new MySqlConnection(connectionString);
            conn.Open();

            const string sql = @"
                UPDATE attendance
                SET employee_id = @employeeId,
                    date = @date,
                    time_in = @timeIn,
                    time_out = @timeOut,
                    status = @status
                WHERE id = @id;";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", model.Id);
            cmd.Parameters.AddWithValue("@employeeId", model.EmployeeId);
            cmd.Parameters.Add("@date", MySqlDbType.Date).Value = model.Date.Date;
            cmd.Parameters.Add("@timeIn", MySqlDbType.Time).Value = (object?)model.TimeIn ?? DBNull.Value;
            cmd.Parameters.Add("@timeOut", MySqlDbType.Time).Value = (object?)model.TimeOut ?? DBNull.Value;
            cmd.Parameters.AddWithValue("@status", model.Status ?? "Present");
            cmd.ExecuteNonQuery();
        }

        public void DeleteAttendance(int id)
        {
            using var conn = new MySqlConnection(connectionString);
            conn.Open();

            const string sql = "DELETE FROM attendance WHERE id = @id;";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }
    }
}
