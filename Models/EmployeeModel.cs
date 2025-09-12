#nullable enable
using System;

namespace HillsCafeManagement.Models
{
    /// <summary>
    /// HR-facing employee profile. Login status is owned by the linked UserAccount.
    /// </summary>
    public class EmployeeModel
    {
        public int Id { get; set; }

        // ---- Identity & Contact ----
        public string? FullName { get; set; } = string.Empty;
        public int? Age { get; set; }
        public string? Sex { get; set; } = string.Empty;
        public string? Address { get; set; } = string.Empty;
        public DateTime? Birthday { get; set; }
        public string? ContactNumber { get; set; } = string.Empty;

        // ---- Job Details ----
        public string? Position { get; set; } = string.Empty;
        public decimal? SalaryPerDay { get; set; }

        /// <summary>
        /// Selected Work Schedule row (FK to work_schedule.id).
        /// </summary>
        public int? WorkScheduleId { get; set; }

        public string? Shift { get; set; } = string.Empty;

        // ---- Government IDs ----
        public string? SssNumber { get; set; } = string.Empty;
        public string? PhilhealthNumber { get; set; } = string.Empty;
        public string? PagibigNumber { get; set; } = string.Empty;

        // ---- Misc ----
        public string? ImageUrl { get; set; } = string.Empty;
        public string? EmergencyContact { get; set; } = string.Empty;

        public DateTime? DateHired { get; set; }
        public DateTime CreatedAt { get; set; }

        // ---- Login linkage (owned by users table) ----
        public UserModel? UserAccount { get; set; }

        /// <summary>
        /// Convenience mirror of the linked user's login status:
        /// true = Active (can log in), false = Inactive (cannot log in), null = no linked user.
        /// </summary>
        public bool? IsActive => UserAccount?.IsActive;

        /// <summary>
        /// "Active" / "Inactive" / "No Login" (for UI display).
        /// </summary>
        public string StatusText =>
            UserAccount is null ? "No Login" :
            (UserAccount.IsActive ? "Active" : "Inactive");

        /// <summary>
        /// Whether this employee has a linked login record.
        /// </summary>
        public bool HasLogin => UserAccount is not null;
    }
}
