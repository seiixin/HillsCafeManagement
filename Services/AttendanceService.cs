#nullable enable
using HillsCafeManagement.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Runtime.CompilerServices;

namespace HillsCafeManagement.Services
{
    /// <summary>
    /// Exception that includes a precise reason + diagnostics + source file/line.
    /// </summary>
    public sealed class AttendanceValidationException : InvalidOperationException
    {
        public string Diagnostics { get; }
        public string CallerMember { get; }
        public string CallerFile { get; }
        public int CallerLine { get; }

        public AttendanceValidationException(
            string message,
            string diagnostics,
            string callerMember,
            string callerFile,
            int callerLine) : base(message)
        {
            Diagnostics = diagnostics;
            CallerMember = callerMember;
            CallerFile = callerFile;
            CallerLine = callerLine;
        }

        public override string Message =>
            base.Message +
            Environment.NewLine +
            Diagnostics +
            Environment.NewLine +
            $"@ {CallerMember} ({System.IO.Path.GetFileName(CallerFile)}:{CallerLine})";
    }

    public class AttendanceService
    {
        private readonly string connectionString = "server=localhost;user=root;password=;database=hillscafe_db;";

        // =========================
        // Auto-NOW versions (no timestamp passed)
        // =========================

        /// <summary>
        /// Inserts a new attendance row with CURDATE()/CURTIME() as time_in.
        /// Blocks if outside employee's assigned work schedule/shift.
        /// </summary>
        public void ClockIn(int employeeId)
        {
            ValidateWithinScheduledShiftOrThrow(employeeId, DateTime.Now);
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
        /// Sets time_out = CURTIME() for the most recent open record (today, no time_out yet).
        /// Blocks if outside employee's assigned work schedule/shift.
        /// </summary>
        public void ClockOut(int employeeId)
        {
            ValidateWithinScheduledShiftOrThrow(employeeId, DateTime.Now);

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
        /// Inserts a new row using the provided timestamp.
        /// Blocks if outside employee's assigned work schedule/shift at that time.
        /// </summary>
        public void ClockIn(int employeeId, DateTime timestamp)
        {
            ValidateWithinScheduledShiftOrThrow(employeeId, timestamp);

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
        /// Blocks if outside employee's assigned work schedule/shift at that time.
        /// </summary>
        public void ClockOut(int employeeId, DateTime timestamp)
        {
            ValidateWithinScheduledShiftOrThrow(employeeId, timestamp);

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

        public List<AttendanceModel> GetAttendancesForEmployee(int employeeId, DateTime? from = null, DateTime? to = null)
        {
            var results = new List<AttendanceModel>();
            using var conn = new MySqlConnection(connectionString);
            conn.Open();

            var sql = @"
                SELECT id, employee_id, date, time_in, time_out, status
                FROM attendance
                WHERE employee_id = @employeeId";

            if (from.HasValue && to.HasValue)
                sql += " AND date BETWEEN @from AND @to";
            else if (from.HasValue)
                sql += " AND date >= @from";
            else if (to.HasValue)
                sql += " AND date <= @to";

            sql += " ORDER BY date DESC, time_in DESC, id DESC";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@employeeId", employeeId);
            if (from.HasValue) cmd.Parameters.Add("@from", MySqlDbType.Date).Value = from.Value.Date;
            if (to.HasValue) cmd.Parameters.Add("@to", MySqlDbType.Date).Value = to.Value.Date;

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

        // =========================
        // Work-schedule / Shift gate WITH DIAGNOSTICS
        // =========================

        private void ValidateWithinScheduledShiftOrThrow(
            int employeeId,
            DateTime when,
            [CallerMemberName] string callerMember = "",
            [CallerFilePath] string callerFile = "",
            [CallerLineNumber] int callerLine = 0)
        {
            // 1) Load employee’s work schedule + shift
            int? workScheduleId = null;
            string? shiftName = null;

            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                const string empSql = @"SELECT work_schedule_id, shift FROM employees WHERE id = @id LIMIT 1;";
                using var empCmd = new MySqlCommand(empSql, conn);
                empCmd.Parameters.AddWithValue("@id", employeeId);
                using var r = empCmd.ExecuteReader();
                if (!r.Read())
                {
                    ThrowDetailed("Employee not found.",
                        $"employee_id={employeeId}",
                        callerMember, callerFile, callerLine);
                }

                workScheduleId = r.IsDBNull(r.GetOrdinal("work_schedule_id"))
                    ? (int?)null
                    : Convert.ToInt32(r["work_schedule_id"]);
                shiftName = r["shift"]?.ToString();
            }

            if (workScheduleId is null)
            {
                ThrowDetailed("No work schedule assigned.",
                    $"employee_id={employeeId}, shift='{shiftName ?? "(null)"}'",
                    callerMember, callerFile, callerLine);
            }

            if (string.IsNullOrWhiteSpace(shiftName))
            {
                ThrowDetailed("No shift assigned.",
                    $"employee_id={employeeId}, work_schedule_id={workScheduleId}",
                    callerMember, callerFile, callerLine);
            }

            // 2) Load days_mask
            byte daysMask;
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                const string wsSql = @"SELECT days_mask FROM work_schedule WHERE id = @id LIMIT 1;";
                using var wsCmd = new MySqlCommand(wsSql, conn);
                wsCmd.Parameters.AddWithValue("@id", workScheduleId!.Value);
                var obj = wsCmd.ExecuteScalar();
                if (obj == null || obj == DBNull.Value)
                {
                    ThrowDetailed("Work schedule not found for employee.",
                        $"employee_id={employeeId}, work_schedule_id={workScheduleId}",
                        callerMember, callerFile, callerLine);
                }
                daysMask = Convert.ToByte(obj);
            }

            // 3) Determine shift window (DB overrides -> fallback to defaults)
            var (start, end, source) = ResolveShiftWindow(shiftName!);

            // 4) Check day + time
            var today = when.DayOfWeek;
            int bit = DayOfWeekToBit(today);
            bool dayAllowed = (daysMask & (1 << bit)) != 0;

            // If crossing midnight and current TOD < end, evaluate previous day instead.
            bool crossesMidnight = end <= start;
            var tod = when.TimeOfDay;
            if (crossesMidnight && tod < end)
            {
                var prev = when.AddDays(-1).DayOfWeek;
                bit = DayOfWeekToBit(prev);
                dayAllowed = (daysMask & (1 << bit)) != 0;
            }

            bool timeAllowed = IsWithinWindow(tod, start, end);

            if (!(dayAllowed && timeAllowed))
            {
                var diag =
                    $"now={when:yyyy-MM-dd HH:mm:ss}, tod={tod:hh\\:mm\\:ss}, " +
                    $"shift='{shiftName}', window={FormatWindow(start, end)} (source={source}, crossesMidnight={crossesMidnight}), " +
                    $"mask={FormatMask(daysMask)}, dayCheckedBit={bit}, dayAllowed={dayAllowed}, timeAllowed={timeAllowed}";

                ThrowDetailed("You can only time in/out during your scheduled shift.", diag,
                    callerMember, callerFile, callerLine);
            }
        }

        private static void ThrowDetailed(
            string message,
            string diagnostics,
            string callerMember,
            string callerFile,
            int callerLine)
        {
            throw new AttendanceValidationException(message, diagnostics, callerMember, callerFile, callerLine);
        }

        private static int DayOfWeekToBit(DayOfWeek d) => d switch
        {
            DayOfWeek.Monday => 0,
            DayOfWeek.Tuesday => 1,
            DayOfWeek.Wednesday => 2,
            DayOfWeek.Thursday => 3,
            DayOfWeek.Friday => 4,
            DayOfWeek.Saturday => 5,
            DayOfWeek.Sunday => 6,
            _ => 0
        };

        private static bool IsWithinWindow(TimeSpan t, TimeSpan start, TimeSpan end)
        {
            // If end <= start, window crosses midnight (e.g., 22:00..06:00)
            if (end <= start)
                return t >= start || t < end;
            return t >= start && t <= end;
        }

        private static string FormatMask(byte mask)
        {
            string[] names = { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
            var parts = new List<string>();
            for (int i = 0; i < 7; i++)
                if ((mask & (1 << i)) != 0) parts.Add(names[i]);
            return parts.Count == 0 ? "(none)" : string.Join(" ", parts);
        }

        private static string FormatWindow(TimeSpan start, TimeSpan end)
            => $"{start:hh\\:mm}-{end:hh\\:mm}";

        /// <summary>
        /// Tries DB table `shift_definitions(name,start_time,end_time)` first, then common defaults.
        /// Returns (start,end,source) where source is 'db' or 'default'.
        /// </summary>
        private (TimeSpan start, TimeSpan end, string source) ResolveShiftWindow(string raw)
        {
            var key = (raw ?? "").Trim().ToLowerInvariant();

            // 1) Try DB override (table is optional; swallow if missing)
            try
            {
                using var conn = new MySqlConnection(connectionString);
                conn.Open();
                const string s = @"SELECT start_time, end_time
                                   FROM shift_definitions
                                   WHERE LOWER(name) = LOWER(@n)
                                   LIMIT 1;";
                using var cmd = new MySqlCommand(s, conn);
                cmd.Parameters.AddWithValue("@n", key);
                using var r = cmd.ExecuteReader();
                if (r.Read())
                {
                    var start = r.GetTimeSpan("start_time");
                    var end = r.GetTimeSpan("end_time");
                    return (start, end, "db");
                }
            }
            catch
            {
                // table may not exist; ignore and fallback
            }

            // 2) Defaults / aliases
            if (key is "morning" or "am" or "day" or "day shift")
                return (TimeSpan.FromHours(8), TimeSpan.FromHours(17), "default");    // 08:00–17:00

            if (key is "afternoon" or "pm" or "swing")
                return (TimeSpan.FromHours(13), TimeSpan.FromHours(22), "default");   // 13:00–22:00

            if (key is "night" or "graveyard" or "evening" or "night shift")
                return (TimeSpan.FromHours(22), TimeSpan.FromHours(6), "default");    // 22:00–06:00 (cross-midnight)

            // 3) Final fallback: strict classic day shift
            return (TimeSpan.FromHours(8), TimeSpan.FromHours(17), "default(fallback)");
        }
    }
}
