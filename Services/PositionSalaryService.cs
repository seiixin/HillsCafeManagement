// HillsCafeManagement.Services.PositionSalaryService (refactored, safer init)
// - No schema work in constructor
// - TryEnsureSchema() + IsDbReady() expose errors without throwing
// - Load() never throws; it sets LastError and returns whatever it could load
// - Save() remains transactional and throws to surface real write errors
// Drop-in replacement for your current PositionSalaryService.cs

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using MySql.Data.MySqlClient;

namespace HillsCafeManagement.Services
{
    /// <summary>
    /// DB-backed service for managing Position → Daily Rate presets.
    /// Table: `position_salary`
    /// </summary>
    public sealed class PositionSalaryService
    {
        public class PositionSalary : INotifyPropertyChanged
        {
            private string _position = string.Empty;
            private decimal _dailyRate;
            private bool _isActive = true;
            private DateTime _updatedAt = DateTime.Now;

            public string Position
            {
                get => _position;
                set { if (_position != value) { _position = value; OnPropertyChanged(); } }
            }

            public decimal DailyRate
            {
                get => _dailyRate;
                set { if (_dailyRate != value) { _dailyRate = value; OnPropertyChanged(); } }
            }

            public bool IsActive
            {
                get => _isActive;
                set { if (_isActive != value) { _isActive = value; OnPropertyChanged(); } }
            }

            public DateTime UpdatedAt
            {
                get => _updatedAt;
                set { if (_updatedAt != value) { _updatedAt = value; OnPropertyChanged(); } }
            }

            public event PropertyChangedEventHandler? PropertyChanged;
            private void OnPropertyChanged([CallerMemberName] string? name = null)
                => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private readonly string _connectionString;
        public string? LastError { get; private set; }

        public PositionSalaryService(string? connectionString = null)
        {
            _connectionString = string.IsNullOrWhiteSpace(connectionString)
                ? "server=localhost;user=root;password=;database=hillscafe_db;"
                : connectionString!;
            // IMPORTANT: no EnsureSchema() here — avoid side effects in ctor
        }

        /// <summary>
        /// Creates the table if missing. Never throws; sets LastError and returns false on failure.
        /// Tries a TIMESTAMP-based DDL first, then falls back to DATETIME for older servers.
        /// </summary>
        public bool TryEnsureSchema()
        {
            LastError = null;
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                const string ddlPreferred = @"
                    CREATE TABLE IF NOT EXISTS `position_salary` (
                        `id`         BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
                        `position`   VARCHAR(100)    NOT NULL,
                        `daily_rate` DECIMAL(12,2)   NOT NULL DEFAULT 0,
                        `is_active`  TINYINT(1)      NOT NULL DEFAULT 1,
                        `updated_at` TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP
                                                     ON UPDATE CURRENT_TIMESTAMP,
                        PRIMARY KEY (`id`),
                        UNIQUE KEY `uq_position` (`position`)
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";

                using (var cmd = new MySqlCommand(ddlPreferred, conn))
                    cmd.ExecuteNonQuery();

                return true;
            }
            catch (MySqlException)
            {
                try
                {
                    using var conn = new MySqlConnection(_connectionString);
                    conn.Open();

                    // Fallback: use DATETIME for maximum compatibility.
                    // We set UpdatedAt in code when saving.
                    const string ddlFallback = @"
                        CREATE TABLE IF NOT EXISTS `position_salary` (
                            `id`         BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
                            `position`   VARCHAR(100)    NOT NULL,
                            `daily_rate` DECIMAL(12,2)   NOT NULL DEFAULT 0,
                            `is_active`  TINYINT(1)      NOT NULL DEFAULT 1,
                            `updated_at` DATETIME        NOT NULL,
                            PRIMARY KEY (`id`),
                            UNIQUE KEY `uq_position` (`position`)
                        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";

                    using (var cmd2 = new MySqlCommand(ddlFallback, conn))
                        cmd2.ExecuteNonQuery();

                    return true;
                }
                catch (Exception ex2)
                {
                    LastError = ex2.Message;
                    return false;
                }
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Quick probe — never throws. Returns true if SELECT works.
        /// </summary>
        public bool IsDbReady()
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();
                using var cmd = new MySqlCommand("SELECT COUNT(*) FROM `position_salary`;", conn);
                _ = cmd.ExecuteScalar();
                return true;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return false;
            }
        }

        public ObservableCollection<PositionSalary> Load()
        {
            var list = new ObservableCollection<PositionSalary>();
            LastError = null;

            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                const string sql = @"
                    SELECT `position`, `daily_rate`, `is_active`, `updated_at`
                    FROM `position_salary`
                    ORDER BY `position` ASC;";

                using var cmd = new MySqlCommand(sql, conn);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    var ia = r["is_active"];
                    list.Add(new PositionSalary
                    {
                        Position = r["position"]?.ToString() ?? string.Empty,
                        DailyRate = r.IsDBNull(r.GetOrdinal("daily_rate")) ? 0m : r.GetDecimal("daily_rate"),
                        IsActive = ia != DBNull.Value && Convert.ToInt32(ia) != 0,
                        UpdatedAt = r.IsDBNull(r.GetOrdinal("updated_at")) ? DateTime.Now : r.GetDateTime("updated_at"),
                    });
                }
            }
            catch (Exception ex)
            {
                // Do not throw — let the UI stay open and show a message
                LastError = ex.Message;
            }

            return list;
        }

        /// <summary>
        /// Upserts items by Position (UNIQUE). Normalizes, dedupes, and writes in a transaction.
        /// Throws on real write errors so callers can surface them.
        /// </summary>
        public void Save(IEnumerable<PositionSalary> items)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();
            using var tx = conn.BeginTransaction();

            const string upsert = @"
                INSERT INTO `position_salary` (`position`, `daily_rate`, `is_active`, `updated_at`)
                VALUES (@position, @rate, @active, @updated)
                ON DUPLICATE KEY UPDATE
                    `daily_rate` = VALUES(`daily_rate`),
                    `is_active`  = VALUES(`is_active`),
                    `updated_at` = VALUES(`updated_at`);";

            using var cmd = new MySqlCommand(upsert, conn, tx);
            cmd.Parameters.Add("@position", MySqlDbType.VarChar, 100);
            cmd.Parameters.Add("@rate", MySqlDbType.Decimal);
            cmd.Parameters["@rate"].Precision = 12; cmd.Parameters["@rate"].Scale = 2;
            cmd.Parameters.Add("@active", MySqlDbType.Byte);
            cmd.Parameters.Add("@updated", MySqlDbType.DateTime);

            try
            {
                var normalized = items
                    .Where(x => !string.IsNullOrWhiteSpace(x.Position))
                    .Select(x =>
                    {
                        var pos = x.Position.Trim();
                        var rate = x.DailyRate < 0 ? 0 : Math.Round(x.DailyRate, 2, MidpointRounding.AwayFromZero);
                        var when = x.UpdatedAt == default ? DateTime.Now : x.UpdatedAt;
                        return new PositionSalary { Position = pos, DailyRate = rate, IsActive = x.IsActive, UpdatedAt = when };
                    })
                    .GroupBy(x => x.Position, StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.OrderByDescending(i => i.UpdatedAt).First());

                foreach (var it in normalized)
                {
                    cmd.Parameters["@position"].Value = it.Position;
                    cmd.Parameters["@rate"].Value = it.DailyRate;
                    cmd.Parameters["@active"].Value = (byte)(it.IsActive ? 1 : 0);
                    cmd.Parameters["@updated"].Value = it.UpdatedAt;
                    cmd.ExecuteNonQuery();
                }

                tx.Commit();
            }
            catch
            {
                try { tx.Rollback(); } catch { /* ignore rollback errors */ }
                throw;
            }
        }

        public bool TryGetRate(string? position, out decimal rate)
        {
            rate = 0m;
            if (string.IsNullOrWhiteSpace(position)) return false;

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            const string sql = @"
                SELECT `daily_rate`
                FROM `position_salary`
                WHERE `is_active` = 1 AND `position` = @p
                ORDER BY `updated_at` DESC
                LIMIT 1;";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@p", position.Trim());

            var obj = cmd.ExecuteScalar();
            if (obj == null || obj == DBNull.Value) return false;

            rate = Convert.ToDecimal(obj);
            return true;
        }
    }
}
