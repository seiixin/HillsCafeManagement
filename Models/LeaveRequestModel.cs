namespace HillsCafeManagement.Models
{
    public enum LeaveStatus
    {
        Pending,
        Approved,
        Rejected,
        Cancelled
    }

    public enum LeaveType
    {
        Sick,
        Vacation,
        Emergency,
        Unpaid,
        Other
    }

    public class LeaveRequestModel
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public LeaveType LeaveType { get; set; }      // maps to VARCHAR in DB
        public string? Reason { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public bool HalfDay { get; set; }
        public LeaveStatus Status { get; set; }       // maps to VARCHAR in DB
        public int? ApproverUserId { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
