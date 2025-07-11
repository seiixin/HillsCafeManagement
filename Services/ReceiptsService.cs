using HillsCafeManagement.Models;
using MySql.Data.MySqlClient;
using System.Collections.Generic;

namespace HillsCafeManagement.Services
{
    public static class ReceiptsServices
    {
        private static string connectionString = "server=localhost;user=root;password=;database=hillscafe_db;";

        public static List<ReceiptsModel> GetAllReceipts()
        {
            var receipts = new List<ReceiptsModel>();

            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                    SELECT 
                        r.id AS ReceiptId, 
                        r.order_id AS OrderId, 
                        c.table_id AS TableNumber, 
                        DATE_FORMAT(r.issued_at, '%m/%d/%y') AS Date, 
                        r.amount_paid AS Amount
                    FROM receipts r
                    LEFT JOIN orders o ON r.order_id = o.id
                    LEFT JOIN customers c ON o.customer_id = c.id
                    ORDER BY r.issued_at DESC;
                ";

                using (var cmd = new MySqlCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        receipts.Add(new ReceiptsModel
                        {
                            ReceiptId = reader.GetInt32("ReceiptId"),
                            OrderId = reader.GetInt32("OrderId"),
                            TableNumber = reader.IsDBNull(reader.GetOrdinal("TableNumber")) ? 0 : reader.GetInt32("TableNumber"),
                            Date = reader.GetString("Date"),
                            Amount = reader.GetDecimal("Amount")
                        });
                    }
                }
            }

            return receipts;
        }
    }
}
