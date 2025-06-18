using System;

namespace HillsCafeManagement.Models
{
    public class EmployeeModel
    {
        public int Id { get; set; }
        public string? FullName { get; set; } = string.Empty;
        public int? Age { get; set; }
        public string? Sex { get; set; } = string.Empty;
        public string? Address { get; set; } = string.Empty;
        public DateTime? Birthday { get; set; }
        public string? ContactNumber { get; set; } = string.Empty;
        public string? Position { get; set; } = string.Empty;
        public decimal? SalaryPerDay { get; set; }
        public string? Shift { get; set; } = string.Empty;
        public string? SssNumber { get; set; } = string.Empty;
        public string? PhilhealthNumber { get; set; } = string.Empty;
        public string? PagibigNumber { get; set; } = string.Empty;
        public string? ImageUrl { get; set; } = string.Empty;
        public string? EmergencyContact { get; set; } = string.Empty;
        public DateTime? DateHired { get; set; }
        public DateTime CreatedAt { get; set; }

        // Link to user login
        public UserModel? UserAccount { get; set; }
    }
}
