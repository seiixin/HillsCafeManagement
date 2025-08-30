using HillsCafeManagement.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HillsCafeManagement.Services
{
    public class OrderService
    {
        private readonly string _connectionString =
            "server=localhost;user=root;password=;database=hillscafe_db;";

        // ---------------- READ (Orders + Items) ----------------
        public List<OrderModel> GetAllOrders()
        {
            var orders = new List<OrderModel>();

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();

                const string query = @"
                    SELECT 
                        o.id,
                        o.customer_id,
                        o.table_number,
                        o.total_amount,
                        o.payment_status,
                        o.created_at,
                        o.cash_register_id,
                        o.order_status,
                        o.ordered_by_user_id
                    FROM orders o
                    ORDER BY o.created_at DESC";

                using (var cmd = new MySqlCommand(query, connection))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        // payment_status safe read
                        string? paymentStatusStr = reader.IsDBNull(reader.GetOrdinal("payment_status"))
                            ? null
                            : reader.GetString("payment_status");

                        // order_status safe read
                        string? orderStatusStr = reader.IsDBNull(reader.GetOrdinal("order_status"))
                            ? null
                            : reader.GetString("order_status");

                        // total_amount safe read
                        decimal total = reader.IsDBNull(reader.GetOrdinal("total_amount"))
                            ? 0m
                            : reader.GetDecimal("total_amount");

                        var model = new OrderModel
                        {
                            Id = reader.GetInt32("id"),
                            CustomerId = reader.IsDBNull(reader.GetOrdinal("customer_id"))
                                ? null : reader.GetInt32("customer_id"),
                            TableNumber = reader.IsDBNull(reader.GetOrdinal("table_number"))
                                ? null : reader.GetString("table_number"),
                            TotalAmount = total,
                            PaymentStatus = Enum.TryParse(paymentStatusStr ?? "", true, out PaymentStatus ps)
                                ? ps : PaymentStatus.Unpaid,
                            CreatedAt = reader.GetDateTime("created_at"),
                            CashRegisterId = reader.IsDBNull(reader.GetOrdinal("cash_register_id"))
                                ? null : reader.GetInt32("cash_register_id"),
                            OrderStatus = Enum.TryParse(orderStatusStr ?? "", true, out OrderStatus os)
                                ? os : OrderStatus.Pending,
                            OrderedByUserId = reader.IsDBNull(reader.GetOrdinal("ordered_by_user_id"))
                                ? null : reader.GetInt32("ordered_by_user_id"),
                            Items = new List<OrderItemModel>() // filled below
                        };

                        orders.Add(model);
                    }
                }
            }

            // Load items for each order (simple & clear; optimize later if needed)
            foreach (var order in orders)
                order.Items = GetOrderItems(order.Id);

            return orders;
        }

        public List<OrderItemModel> GetOrderItems(int orderId)
        {
            var items = new List<OrderItemModel>();

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();

                const string query = @"
                    SELECT 
                        oi.id,
                        oi.order_id,
                        oi.product_id,
                        oi.quantity,
                        oi.unit_price,
                        m.name AS product_name,
                        m.category
                    FROM order_items oi
                    INNER JOIN menu m ON oi.product_id = m.id
                    WHERE oi.order_id = @orderId";

                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@orderId", orderId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            items.Add(new OrderItemModel
                            {
                                Id = reader.GetInt32("id"),
                                OrderId = reader.GetInt32("order_id"),
                                ProductId = reader.GetInt32("product_id"),
                                Quantity = reader.GetInt32("quantity"),
                                UnitPrice = reader.IsDBNull(reader.GetOrdinal("unit_price"))
                                    ? 0m : reader.GetDecimal("unit_price"),
                                ProductName = reader.IsDBNull(reader.GetOrdinal("product_name"))
                                    ? null : reader.GetString("product_name"),
                                Category = reader.IsDBNull(reader.GetOrdinal("category"))
                                    ? null : reader.GetString("category")
                            });
                        }
                    }
                }
            }

            return items;
        }

        // ---------------- CREATE ----------------
        public int AddOrder(OrderModel order)
        {
            // Ensure totals are in sync with items
            if (order.Items != null && order.Items.Count > 0)
            {
                order.TotalAmount = order.Items.Sum(x => x.UnitPrice * x.Quantity);
            }

            // Ensure CreatedAt is sane
            if (order.CreatedAt == default)
                order.CreatedAt = DateTime.Now;

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        const string insertOrder = @"
                            INSERT INTO orders 
                                (customer_id, table_number, total_amount, payment_status, created_at, cash_register_id, order_status, ordered_by_user_id)
                            VALUES
                                (@customerId, @tableNumber, @totalAmount, @paymentStatus, @createdAt, @cashRegisterId, @orderStatus, @orderedByUserId);
                            SELECT LAST_INSERT_ID();";

                        int newOrderId;
                        using (var cmd = new MySqlCommand(insertOrder, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@customerId", (object?)order.CustomerId ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@tableNumber", (object?)order.TableNumber ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@totalAmount", order.TotalAmount);
                            cmd.Parameters.AddWithValue("@paymentStatus", order.PaymentStatus.ToString());
                            cmd.Parameters.AddWithValue("@createdAt", order.CreatedAt);
                            cmd.Parameters.AddWithValue("@cashRegisterId", (object?)order.CashRegisterId ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@orderStatus", order.OrderStatus.ToString());
                            cmd.Parameters.AddWithValue("@orderedByUserId", (object?)order.OrderedByUserId ?? DBNull.Value);

                            newOrderId = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        if (order.Items != null)
                        {
                            foreach (var item in order.Items)
                                AddOrderItem(connection, transaction, newOrderId, item);
                        }

                        transaction.Commit();
                        return newOrderId;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        private void AddOrderItem(MySqlConnection connection, MySqlTransaction tx, int orderId, OrderItemModel item)
        {
            const string insertItem = @"
                INSERT INTO order_items (order_id, product_id, quantity, unit_price)
                VALUES (@orderId, @productId, @quantity, @unitPrice)";

            using (var cmd = new MySqlCommand(insertItem, connection, tx))
            {
                cmd.Parameters.AddWithValue("@orderId", orderId);
                cmd.Parameters.AddWithValue("@productId", item.ProductId);
                cmd.Parameters.AddWithValue("@quantity", item.Quantity);
                cmd.Parameters.AddWithValue("@unitPrice", item.UnitPrice);
                cmd.ExecuteNonQuery();
            }
        }

        // ---------------- UPDATE ----------------
        public void UpdateOrder(OrderModel order)
        {
            // Keep totals in sync
            if (order.Items != null && order.Items.Count > 0)
            {
                order.TotalAmount = order.Items.Sum(x => x.UnitPrice * x.Quantity);
            }

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                using (var tx = connection.BeginTransaction())
                {
                    try
                    {
                        const string updateOrder = @"
                            UPDATE orders SET
                                customer_id=@customerId,
                                table_number=@tableNumber,
                                total_amount=@totalAmount,
                                payment_status=@paymentStatus,
                                cash_register_id=@cashRegisterId,
                                order_status=@orderStatus,
                                ordered_by_user_id=@orderedByUserId
                            WHERE id=@id";

                        using (var cmd = new MySqlCommand(updateOrder, connection, tx))
                        {
                            cmd.Parameters.AddWithValue("@id", order.Id);
                            cmd.Parameters.AddWithValue("@customerId", (object?)order.CustomerId ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@tableNumber", (object?)order.TableNumber ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@totalAmount", order.TotalAmount);
                            cmd.Parameters.AddWithValue("@paymentStatus", order.PaymentStatus.ToString());
                            cmd.Parameters.AddWithValue("@cashRegisterId", (object?)order.CashRegisterId ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@orderStatus", order.OrderStatus.ToString());
                            cmd.Parameters.AddWithValue("@orderedByUserId", (object?)order.OrderedByUserId ?? DBNull.Value);
                            cmd.ExecuteNonQuery();
                        }

                        // Simple strategy: replace all items
                        using (var cmdDel = new MySqlCommand("DELETE FROM order_items WHERE order_id=@id", connection, tx))
                        {
                            cmdDel.Parameters.AddWithValue("@id", order.Id);
                            cmdDel.ExecuteNonQuery();
                        }

                        if (order.Items != null)
                        {
                            foreach (var item in order.Items)
                                AddOrderItem(connection, tx, order.Id, item);
                        }

                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }

        // ---------------- DELETE ----------------
        public void DeleteOrder(int orderId)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                using (var tx = connection.BeginTransaction())
                {
                    try
                    {
                        using (var cmd = new MySqlCommand("DELETE FROM order_items WHERE order_id=@id", connection, tx))
                        {
                            cmd.Parameters.AddWithValue("@id", orderId);
                            cmd.ExecuteNonQuery();
                        }

                        using (var cmd = new MySqlCommand("DELETE FROM orders WHERE id=@id", connection, tx))
                        {
                            cmd.Parameters.AddWithValue("@id", orderId);
                            cmd.ExecuteNonQuery();
                        }

                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }

        // ---------------- MENU PRODUCTS (for dropdown) ----------------
        public List<MenuModel> GetAllMenu()
        {
            var products = new List<MenuModel>();

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                const string query = "SELECT id, name, category, price FROM menu ORDER BY name";

                using (var cmd = new MySqlCommand(query, connection))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        products.Add(new MenuModel
                        {
                            Id = reader.GetInt32("id"),
                            Name = reader.IsDBNull(reader.GetOrdinal("name"))
                                ? string.Empty : reader.GetString("name"),
                            Category = reader.IsDBNull(reader.GetOrdinal("category"))
                                ? string.Empty : reader.GetString("category"),
                            // MenuModel.Price is nullable, so handle NULLs:
                            Price = reader.IsDBNull(reader.GetOrdinal("price"))
                                ? (decimal?)null : reader.GetDecimal("price")
                        });
                    }
                }
            }

            return products;
        }
    }
}
