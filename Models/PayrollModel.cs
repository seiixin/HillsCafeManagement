namespace HillsCafeManagement.Models
{
    public class PayrollModel
    {
        public int Id { get; set; }
        public string EmployeeId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string PayDate { get; set; } = string.Empty;
        public int HoursWorked { get; set; }
        public decimal RatePerHour { get; set; }
        public decimal Deductions { get; set; }
        public decimal NetSalary { get; set; }
    }
}
