using HillsCafeManagement.Models;
using MySql.Data.MySqlClient;
using System.Collections.Generic;

namespace HillsCafeManagement.Services
{
    public class OrderService
    {
        private readonly string _connectionString = "server=localhost;user=root;password=;database=hillscafe_db;";

        public List<OrderModel> GetAllOrders()
        {
            var orders = new List<OrderModel>();

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();

                // Updated query to get created_at, cashier name, table number and status
                var query = @"
                    SELECT 
                        o.id, 
                        o.created_at, 
                        o.order_status, 
                        e.full_name AS cashier_name,
                        ct.table_number
                    FROM orders o
                    LEFT JOIN users u ON o.ordered_by_user_id = u.id
                    LEFT JOIN employees e ON u.employee_id = e.id
                    LEFT JOIN customers c ON o.customer_id = c.id
                    LEFT JOIN cafe_tables ct ON c.table_id = ct.id
                    ORDER BY o.created_at DESC";

                using (var cmd = new MySqlCommand(query, connection))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        orders.Add(new OrderModel
                        {
                            Id = reader.GetInt32("id"),
                            Date = reader.GetDateTime("created_at").ToString("yyyy-MM-dd HH:mm:ss"),
                            TableNumber = reader.IsDBNull(reader.GetOrdinal("table_number"))
                                ? ""
                                : reader.GetString("table_number"),
                            CashierName = reader.IsDBNull(reader.GetOrdinal("cashier_name"))
                                ? ""
                                : reader.GetString("cashier_name"),
                            Status = reader.GetString("order_status")
                        });
                    }
                }
            }

            return orders;
        }
    }
}
