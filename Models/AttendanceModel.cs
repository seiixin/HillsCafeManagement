#nullable enable
using System;

namespace HillsCafeManagement.Models
{
    /// <summary>
    /// Typed model for the attendance table.
    /// Matches columns commonly used in queries:
    ///   attendance(id, employee_id, date, time_in, time_out, status, created_at?)
    /// </summary>
    public sealed class AttendanceModel
    {
        public int Id { get; set; }

        /// <summary>FK → employees.id</summary>
        public int EmployeeId { get; set; }

        /// <summary>
        /// The calendar date of the shift (date-only; time component should be 00:00:00).
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>MySQL TIME → C# TimeSpan</summary>
        public TimeSpan? TimeIn { get; set; }

        /// <summary>MySQL TIME → C# TimeSpan</summary>
        public TimeSpan? TimeOut { get; set; }

        /// <summary>
        /// Optional status label (e.g., "Present", "Absent", "Late"). Can be null/empty.
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Optional audit field if your schema stores it.
        /// </summary>
        public DateTime? CreatedAt { get; set; }

        // ---------- Convenience / computed ----------

        /// <summary>True when BOTH time_in AND time_out are present (a completed shift).</summary>
        public bool HasCompleteShift => TimeIn.HasValue && TimeOut.HasValue;

        /// <summary>Date + TimeIn (if set), else null.</summary>
        public DateTime? DateTimeIn => TimeIn.HasValue ? Date.Date.Add(TimeIn.Value) : (DateTime?)null;

        /// <summary>Date + TimeOut (if set), else null.</summary>
        public DateTime? DateTimeOut => TimeOut.HasValue ? Date.Date.Add(TimeOut.Value) : (DateTime?)null;

        /// <summary>
        /// Total hours worked for the row (0 if incomplete or invalid). Does not handle overnight cross-date shifts.
        /// </summary>
        public double HoursWorked
            => (HasCompleteShift && DateTimeOut > DateTimeIn)
                ? (DateTimeOut!.Value - DateTimeIn!.Value).TotalHours
                : 0.0;
    }
}
