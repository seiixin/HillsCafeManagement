namespace HillsCafeManagement.Models
{
    public class EmployeeModel
    {
        public int Id { get; set; }
        public string? FullName { get; set; } = string.Empty;
        public string? Email { get; set; } = string.Empty;
        public string? Password { get; set; } = string.Empty;
        public string? Position { get; set; } = string.Empty; 
    }
}
