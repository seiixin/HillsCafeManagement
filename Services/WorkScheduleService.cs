using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using HillsCafeManagement.Models;
using MySql.Data.MySqlClient;

namespace HillsCafeManagement.Services
{
    public class WorkScheduleService
    {
        private readonly string _cs;
        public string? LastError { get; private set; }

        public WorkScheduleService(string? connectionString = null)
        {
            _cs = string.IsNullOrWhiteSpace(connectionString)
                ? "server=localhost;user=root;password=;database=hillscafe_db;"
                : connectionString!;
        }

        public bool TryEnsureSchema()
        {
            LastError = null;
            try
            {
                using var conn = new MySqlConnection(_cs);
                conn.Open();

                const string ddl = @"
                    CREATE TABLE IF NOT EXISTS `work_schedule` (
                        `id`         BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
                        `label`      VARCHAR(100)    NOT NULL,
                        `days_mask`  TINYINT UNSIGNED NOT NULL DEFAULT 0, -- bit0=Mon ... bit6=Sun
                        `is_active`  TINYINT(1)      NOT NULL DEFAULT 1,
                        `updated_at` TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP
                                                     ON UPDATE CURRENT_TIMESTAMP,
                        PRIMARY KEY (`id`),
                        UNIQUE KEY `uq_label` (`label`)
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
                using var cmd = new MySqlCommand(ddl, conn);
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return false;
            }
        }

        public bool IsDbReady()
        {
            try
            {
                using var conn = new MySqlConnection(_cs);
                conn.Open();
                using var cmd = new MySqlCommand("SELECT COUNT(*) FROM `work_schedule`;", conn);
                _ = cmd.ExecuteScalar();
                return true;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return false;
            }
        }

        public ObservableCollection<WorkScheduleModel> Load()
        {
            var list = new ObservableCollection<WorkScheduleModel>();
            LastError = null;

            try
            {
                using var conn = new MySqlConnection(_cs);
                conn.Open();
                const string sql = @"
                    SELECT id, label, days_mask, is_active, updated_at
                    FROM work_schedule
                    ORDER BY label ASC;";
                using var cmd = new MySqlCommand(sql, conn);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    var m = new WorkScheduleModel
                    {
                        Id = r.GetInt32("id"),
                        Label = r["label"]?.ToString() ?? string.Empty,
                        IsActive = Convert.ToInt32(r["is_active"]) != 0,
                        UpdatedAt = r.GetDateTime("updated_at")
                    };
                    var mask = Convert.ToByte(r["days_mask"]);
                    m.FromDaysMask(mask);
                    list.Add(m);
                }
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
            }

            return list;
        }

        /// <summary>
        /// Saves the list transactionally:
        /// 1) Upsert all items by label
        /// 2) Delete any rows not in provided labels (case-insensitive)
        /// </summary>
        public void Save(IEnumerable<WorkScheduleModel> items)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));

            using var conn = new MySqlConnection(_cs);
            conn.Open();
            using var tx = conn.BeginTransaction();

            const string upsert = @"
                INSERT INTO work_schedule (label, days_mask, is_active, updated_at)
                VALUES (@label, @mask, @active, @updated)
                ON DUPLICATE KEY UPDATE
                    days_mask = VALUES(days_mask),
                    is_active = VALUES(is_active),
                    updated_at = VALUES(updated_at);";

            using var upCmd = new MySqlCommand(upsert, conn, tx);
            upCmd.Parameters.Add("@label", MySqlDbType.VarChar, 100);
            upCmd.Parameters.Add("@mask", MySqlDbType.UByte);
            upCmd.Parameters.Add("@active", MySqlDbType.Byte);
            upCmd.Parameters.Add("@updated", MySqlDbType.DateTime);

            try
            {
                var normalized = items
                    .Where(x => !string.IsNullOrWhiteSpace(x.Label))
                    .Select(x =>
                    {
                        var label = x.Label.Trim();
                        if (label.Length > 100) label = label[..100];
                        return new WorkScheduleModel
                        {
                            Label = label,
                            UpdatedAt = x.UpdatedAt == default ? DateTime.Now : x.UpdatedAt,
                            IsActive = x.IsActive,
                            Mon = x.Mon,
                            Tue = x.Tue,
                            Wed = x.Wed,
                            Thu = x.Thu,
                            Fri = x.Fri,
                            Sat = x.Sat,
                            Sun = x.Sun
                        };
                    })
                    .GroupBy(x => x.Label, StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.OrderByDescending(i => i.UpdatedAt).First())
                    .ToList();

                foreach (var it in normalized)
                {
                    upCmd.Parameters["@label"].Value = it.Label;
                    upCmd.Parameters["@mask"].Value = it.ToDaysMask();
                    upCmd.Parameters["@active"].Value = (byte)(it.IsActive ? 1 : 0);
                    upCmd.Parameters["@updated"].Value = it.UpdatedAt == default ? DateTime.Now : it.UpdatedAt;
                    upCmd.ExecuteNonQuery();
                }

                // delete rows not present
                var keep = new HashSet<string>(normalized.Select(n => n.Label), StringComparer.OrdinalIgnoreCase);
                var existing = new List<string>();
                using (var sel = new MySqlCommand("SELECT label FROM work_schedule;", conn, tx))
                using (var rr = sel.ExecuteReader())
                {
                    while (rr.Read()) existing.Add(rr["label"]?.ToString() ?? "");
                }

                var toDelete = existing.Where(x => !string.IsNullOrWhiteSpace(x) && !keep.Contains(x)).ToList();
                if (toDelete.Count > 0)
                {
                    using var del = new MySqlCommand("DELETE FROM work_schedule WHERE label=@l;", conn, tx);
                    del.Parameters.Add("@l", MySqlDbType.VarChar, 100);
                    foreach (var l in toDelete) { del.Parameters["@l"].Value = l; del.ExecuteNonQuery(); }
                }

                tx.Commit();
            }
            catch
            {
                try { tx.Rollback(); } catch { /* ignore */ }
                throw;
            }
        }
    }
}
