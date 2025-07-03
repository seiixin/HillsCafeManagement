using System;

namespace HillsCafeManagement.Models
{
    public class UserModel
    {
        public int Id { get; set; }
        public string? Email { get; set; } = string.Empty;
        public string? Password { get; set; } = string.Empty;
        public string? Role { get; set; } = "Employee";  
        public int? EmployeeId { get; set; }
        public DateTime CreatedAt { get; set; }

        //  link back to Employee
        public EmployeeModel? Employee { get; set; }
    }
}
