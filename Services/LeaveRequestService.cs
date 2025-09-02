using HillsCafeManagement.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace HillsCafeManagement.Services
{
    public interface ILeaveRequestService
    {
        int Create(LeaveRequestModel model);
        bool Update(LeaveRequestModel model);
        bool UpdateStatus(int id, LeaveStatus status, int? approverUserId = null);
        bool Delete(int id);
        LeaveRequestModel? GetById(int id);
        List<LeaveRequestModel> GetForEmployee(int employeeId, DateTime? from = null, DateTime? to = null, LeaveStatus? status = null);
        List<LeaveRequestModel> GetAll(DateTime? from = null, DateTime? to = null, LeaveStatus? status = null);
        bool Approve(int id, int approverUserId);
        bool Reject(int id, int approverUserId);
        bool Cancel(int id);
        bool IsOnApprovedLeave(int employeeId, DateTime date);
    }

    public sealed class LeaveRequestService : ILeaveRequestService
    {
        private readonly string _cs = "server=localhost;user=root;password=;database=hillscafe_db;";

        public int Create(LeaveRequestModel m)
        {
            using var conn = new MySqlConnection(_cs);
            conn.Open();

            const string sql = @"
INSERT INTO leave_requests
(employee_id, leave_type, reason, date_from, date_to, half_day, status, created_at)
VALUES
(@empId, @type, @reason, @from, @to, @half, @status, NOW());
SELECT LAST_INSERT_ID();";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.Add("@empId", MySqlDbType.Int32).Value = m.EmployeeId;
            cmd.Parameters.Add("@type", MySqlDbType.VarChar).Value = m.LeaveType.ToString();
            cmd.Parameters.Add("@reason", MySqlDbType.VarChar).Value = (object?)m.Reason ?? DBNull.Value;
            cmd.Parameters.Add("@from", MySqlDbType.Date).Value = m.DateFrom.Date;
            cmd.Parameters.Add("@to", MySqlDbType.Date).Value = m.DateTo.Date;
            cmd.Parameters.Add("@half", MySqlDbType.Bit).Value = m.HalfDay ? 1 : 0;
            cmd.Parameters.Add("@status", MySqlDbType.VarChar).Value = m.Status.ToString();

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        // Full-field update (includes Status). Typed params + NULL-safe.
        public bool Update(LeaveRequestModel m)
        {
            using var conn = new MySqlConnection(_cs);
            conn.Open();

            const string sql = @"
UPDATE leave_requests
SET
    leave_type = @type,
    reason     = @reason,
    date_from  = @from,
    date_to    = @to,
    half_day   = @half,
    status     = @status,
    updated_at = NOW()
WHERE id = @id;";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.Add("@id", MySqlDbType.Int32).Value = m.Id;
            cmd.Parameters.Add("@type", MySqlDbType.VarChar).Value = m.LeaveType.ToString();
            cmd.Parameters.Add("@reason", MySqlDbType.VarChar).Value = (object?)m.Reason ?? DBNull.Value;
            cmd.Parameters.Add("@from", MySqlDbType.Date).Value = m.DateFrom.Date;
            cmd.Parameters.Add("@to", MySqlDbType.Date).Value = m.DateTo.Date;
            cmd.Parameters.Add("@half", MySqlDbType.Bit).Value = m.HalfDay ? 1 : 0;
            cmd.Parameters.Add("@status", MySqlDbType.VarChar).Value = m.Status.ToString();

            return cmd.ExecuteNonQuery() > 0;
        }

        // Safer when status lang ang nagbago (also writes approver/timestamp if applicable)
        public bool UpdateStatus(int id, LeaveStatus status, int? approverUserId = null)
        {
            using var conn = new MySqlConnection(_cs);
            conn.Open();

            var sql = @"
UPDATE leave_requests
SET status = @status, updated_at = NOW()";

            // set approver + approved_at only for Approved/Rejected
            if (status == LeaveStatus.Approved || status == LeaveStatus.Rejected)
                sql += ", approver_id = @approver, approved_at = NOW()";

            sql += " WHERE id = @id;";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.Add("@id", MySqlDbType.Int32).Value = id;
            cmd.Parameters.Add("@status", MySqlDbType.VarChar).Value = status.ToString();
            if (status == LeaveStatus.Approved || status == LeaveStatus.Rejected)
                cmd.Parameters.Add("@approver", MySqlDbType.Int32).Value = (object?)approverUserId ?? DBNull.Value;

            return cmd.ExecuteNonQuery() > 0;
        }

        public bool Delete(int id)
        {
            using var conn = new MySqlConnection(_cs);
            conn.Open();
            const string sql = "DELETE FROM leave_requests WHERE id = @id;";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.Add("@id", MySqlDbType.Int32).Value = id;
            return cmd.ExecuteNonQuery() > 0;
        }

        public LeaveRequestModel? GetById(int id)
        {
            using var conn = new MySqlConnection(_cs);
            conn.Open();
            const string sql = "SELECT * FROM leave_requests WHERE id = @id LIMIT 1;";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.Add("@id", MySqlDbType.Int32).Value = id;

            using var r = cmd.ExecuteReader();
            return r.Read() ? Map(r) : null;
        }

        public List<LeaveRequestModel> GetForEmployee(int employeeId, DateTime? from = null, DateTime? to = null, LeaveStatus? status = null)
        {
            using var conn = new MySqlConnection(_cs);
            conn.Open();

            var sql = @"SELECT * FROM leave_requests WHERE employee_id = @empId";
            if (from.HasValue) sql += " AND date_to   >= @from";
            if (to.HasValue) sql += " AND date_from <= @to";
            if (status.HasValue) sql += " AND status = @status";
            sql += " ORDER BY date_from DESC, id DESC";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.Add("@empId", MySqlDbType.Int32).Value = employeeId;
            if (from.HasValue) cmd.Parameters.Add("@from", MySqlDbType.Date).Value = from.Value.Date;
            if (to.HasValue) cmd.Parameters.Add("@to", MySqlDbType.Date).Value = to.Value.Date;
            if (status.HasValue) cmd.Parameters.Add("@status", MySqlDbType.VarChar).Value = status.Value.ToString();

            var list = new List<LeaveRequestModel>();
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(Map(r));
            return list;
        }

        public List<LeaveRequestModel> GetAll(DateTime? from = null, DateTime? to = null, LeaveStatus? status = null)
        {
            using var conn = new MySqlConnection(_cs);
            conn.Open();

            var sql = @"SELECT * FROM leave_requests WHERE 1=1";
            if (from.HasValue) sql += " AND date_to   >= @from";
            if (to.HasValue) sql += " AND date_from <= @to";
            if (status.HasValue) sql += " AND status = @status";
            sql += " ORDER BY date_from DESC, id DESC";

            using var cmd = new MySqlCommand(sql, conn);
            if (from.HasValue) cmd.Parameters.Add("@from", MySqlDbType.Date).Value = from.Value.Date;
            if (to.HasValue) cmd.Parameters.Add("@to", MySqlDbType.Date).Value = to.Value.Date;
            if (status.HasValue) cmd.Parameters.Add("@status", MySqlDbType.VarChar).Value = status.Value.ToString();

            var list = new List<LeaveRequestModel>();
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(Map(r));
            return list;
        }

        public bool Approve(int id, int approverUserId) => UpdateStatus(id, LeaveStatus.Approved, approverUserId);
        public bool Reject(int id, int approverUserId) => UpdateStatus(id, LeaveStatus.Rejected, approverUserId);

        public bool Cancel(int id)
        {
            using var conn = new MySqlConnection(_cs);
            conn.Open();
            const string sql = "UPDATE leave_requests SET status = 'Cancelled', updated_at = NOW() WHERE id = @id;";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.Add("@id", MySqlDbType.Int32).Value = id;
            return cmd.ExecuteNonQuery() > 0;
        }

        public bool IsOnApprovedLeave(int employeeId, DateTime date)
        {
            using var conn = new MySqlConnection(_cs);
            conn.Open();
            const string sql = @"
SELECT 1
FROM leave_requests
WHERE employee_id = @empId
  AND status = 'Approved'
  AND @date BETWEEN date_from AND date_to
LIMIT 1;";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.Add("@empId", MySqlDbType.Int32).Value = employeeId;
            cmd.Parameters.Add("@date", MySqlDbType.Date).Value = date.Date;
            using var r = cmd.ExecuteReader();
            return r.Read();
        }

        private static LeaveRequestModel Map(MySqlDataReader r) => new LeaveRequestModel
        {
            Id = r.GetInt32("id"),
            EmployeeId = r.GetInt32("employee_id"),
            LeaveType = Enum.TryParse<LeaveType>(r["leave_type"]?.ToString(), true, out var lt) ? lt : LeaveType.Other,
            Reason = r.IsDBNull(r.GetOrdinal("reason")) ? null : r.GetString("reason"),
            DateFrom = r.GetDateTime("date_from"),
            DateTo = r.GetDateTime("date_to"),
            HalfDay = Convert.ToInt32(r["half_day"]) == 1,
            Status = Enum.TryParse<LeaveStatus>(r["status"]?.ToString(), true, out var ls) ? ls : LeaveStatus.Pending,
            ApproverUserId = r.IsDBNull(r.GetOrdinal("approver_id")) ? (int?)null : r.GetInt32("approver_id"),
            ApprovedAt = r.IsDBNull(r.GetOrdinal("approved_at")) ? (DateTime?)null : r.GetDateTime("approved_at"),
            CreatedAt = r.IsDBNull(r.GetOrdinal("created_at")) ? DateTime.Now : r.GetDateTime("created_at"),
            UpdatedAt = r.IsDBNull(r.GetOrdinal("updated_at")) ? (DateTime?)null : r.GetDateTime("updated_at")
        };
    }
}
