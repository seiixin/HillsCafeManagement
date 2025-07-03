namespace HillsCafeManagement.Models
{
    public class OrderModel
    {
        public int Id { get; set; }
        public string Date { get; set; }
        public string TableNumber { get; set; }
        public string CashierName { get; set; }
        public string Status { get; set; }
    }
}
