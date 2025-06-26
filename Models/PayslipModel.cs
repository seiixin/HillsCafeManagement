using System;

namespace HillsCafeManagement.Models
{
    public class PayslipModel
    {
        // Unique identifier for the payslip
        public int PayslipId { get; set; }

        // Basic employee identification
        public int EmployeeId { get; set; }
        public string FullName { get; set; } = string.Empty;

        // Payslip details
        public DateTime PayDate { get; set; }
        public decimal HoursWorked { get; set; }
        public decimal RatePerHour { get; set; }
        public decimal Deductions { get; set; }
        public decimal NetSalary { get; set; }

        // Status for approval or viewing state (optional)
        public string Status { get; set; } = "Pending";
    }
}
