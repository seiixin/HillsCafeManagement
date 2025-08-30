using System;
using System.Collections.Generic;
using System.Data;
using HillsCafeManagement.Models;
using MySql.Data.MySqlClient;

namespace HillsCafeManagement.Services
{
    public static class ReceiptsServices
    {
        private const string ConnectionString = "server=localhost;user=root;password=;database=hillscafe_db;";

        // ---------- READ all ----------
        public static List<ReceiptsModel> GetAllReceipts(string? searchTerm = null, int? limit = null)
        {
            var receipts = new List<ReceiptsModel>();

            using var conn = new MySqlConnection(ConnectionString);
            conn.Open();

            var sql = @"
                SELECT 
                    r.id AS ReceiptId, 
                    r.order_id AS OrderId, 
                    COALESCE(c.table_id,0) AS TableNumber, 
                    DATE_FORMAT(r.issued_at, '%m/%d/%y') AS Date, 
                    r.amount_paid AS Amount
                FROM receipts r
                LEFT JOIN orders o ON r.order_id = o.id
                LEFT JOIN customers c ON o.customer_id = c.id";

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                sql += @"
                WHERE CAST(r.order_id AS CHAR) LIKE @t
                   OR CAST(COALESCE(c.table_id,0) AS CHAR) LIKE @t
                   OR DATE_FORMAT(r.issued_at, '%m/%d/%y') LIKE @t";
            }

            sql += " ORDER BY r.issued_at DESC";
            if (limit.HasValue && limit > 0) sql += " LIMIT @limit;";

            using var cmd = new MySqlCommand(sql, conn);
            if (!string.IsNullOrWhiteSpace(searchTerm))
                cmd.Parameters.AddWithValue("@t", $"%{searchTerm}%");
            if (limit.HasValue && limit > 0)
                cmd.Parameters.AddWithValue("@limit", limit);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                receipts.Add(Map(reader));
            }

            return receipts;
        }

        // ---------- READ one ----------
        public static ReceiptsModel? GetById(int id)
        {
            using var conn = new MySqlConnection(ConnectionString);
            conn.Open();

            var sql = @"
                SELECT 
                    r.id AS ReceiptId, 
                    r.order_id AS OrderId, 
                    COALESCE(c.table_id,0) AS TableNumber, 
                    DATE_FORMAT(r.issued_at, '%m/%d/%y') AS Date, 
                    r.amount_paid AS Amount
                FROM receipts r
                LEFT JOIN orders o ON r.order_id = o.id
                LEFT JOIN customers c ON o.customer_id = c.id
                WHERE r.id = @id
                LIMIT 1;";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Map(reader) : null;
        }

        // ---------- CREATE ----------
        public static int Create(ReceiptsModel model)
        {
            using var conn = new MySqlConnection(ConnectionString);
            conn.Open();

            var sql = @"
                INSERT INTO receipts (order_id, amount_paid, issued_at)
                VALUES (@orderId, @amount, NOW());
                SELECT LAST_INSERT_ID();";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@orderId", model.OrderId);
            cmd.Parameters.AddWithValue("@amount", model.Amount);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        // ---------- UPDATE ----------
        public static void Update(ReceiptsModel model)
        {
            using var conn = new MySqlConnection(ConnectionString);
            conn.Open();

            var sql = @"
                UPDATE receipts
                SET order_id = @orderId,
                    amount_paid = @amount
                WHERE id = @id;";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@orderId", model.OrderId);
            cmd.Parameters.AddWithValue("@amount", model.Amount);
            cmd.Parameters.AddWithValue("@id", model.ReceiptId);

            cmd.ExecuteNonQuery();
        }

        // ---------- DELETE ----------
        public static void Delete(int id)
        {
            using var conn = new MySqlConnection(ConnectionString);
            conn.Open();

            var sql = "DELETE FROM receipts WHERE id = @id;";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        // ---------- DETAILS: header + line items ----------
        /// <summary>
        /// Returns the receipt header plus its order's line items (product, qty, unit price, subtotal).
        /// Useful for printing/exporting a full receipt.
        /// </summary>
        public static ReceiptDetailsModel? GetDetailsByReceiptId(int receiptId)
        {
            using var conn = new MySqlConnection(ConnectionString);
            conn.Open();

            // 1) Header
            var headerSql = @"
                SELECT 
                    r.id AS ReceiptId,
                    r.order_id AS OrderId,
                    COALESCE(c.table_id,0) AS TableNumber,
                    DATE_FORMAT(r.issued_at, '%m/%d/%y') AS Date,
                    r.amount_paid AS Amount
                FROM receipts r
                LEFT JOIN orders o ON o.id = r.order_id
                LEFT JOIN customers c ON c.id = o.customer_id
                WHERE r.id = @rid
                LIMIT 1;";

            using var cmdHeader = new MySqlCommand(headerSql, conn);
            cmdHeader.Parameters.AddWithValue("@rid", receiptId);

            ReceiptsModel? header = null;
            using (var rh = cmdHeader.ExecuteReader())
            {
                if (rh.Read())
                    header = Map(rh);
            }
            if (header == null) return null;

            var details = new ReceiptDetailsModel { Header = header };

            // 2) Lines (adjust table/column names if yours are different: order_items, menu)
            var linesSql = @"
                SELECT 
                    oi.product_id        AS ProductId,
                    m.name               AS ProductName,
                    oi.quantity          AS Quantity,
                    oi.unit_price        AS UnitPrice
                FROM order_items oi
                INNER JOIN orders o ON o.id = oi.order_id
                LEFT JOIN menu m    ON m.id = oi.product_id
                WHERE o.id = @orderId;";

            using var cmdLines = new MySqlCommand(linesSql, conn);
            cmdLines.Parameters.AddWithValue("@orderId", header.OrderId);

            using var rl = cmdLines.ExecuteReader();
            while (rl.Read())
            {
                var productId = rl.GetInt32(rl.GetOrdinal("ProductId"));
                var productName = rl.IsDBNull(rl.GetOrdinal("ProductName")) ? null : rl.GetString(rl.GetOrdinal("ProductName"));
                var qty = rl.GetInt32(rl.GetOrdinal("Quantity"));
                var unit = rl.GetDecimal(rl.GetOrdinal("UnitPrice"));

                details.Lines.Add(new ReceiptLineModel
                {
                    ProductId = productId,
                    ProductName = productName,
                    Quantity = qty,
                    UnitPrice = unit
                });
            }

            return details;
        }

        // ---------- Mapper ----------
        private static ReceiptsModel Map(IDataRecord r) => new ReceiptsModel
        {
            ReceiptId = r.GetInt32(r.GetOrdinal("ReceiptId")),
            OrderId = r.GetInt32(r.GetOrdinal("OrderId")),
            TableNumber = r.IsDBNull(r.GetOrdinal("TableNumber")) ? 0 : r.GetInt32(r.GetOrdinal("TableNumber")),
            Date = r.IsDBNull(r.GetOrdinal("Date")) ? "" : r.GetString(r.GetOrdinal("Date")),
            Amount = r.IsDBNull(r.GetOrdinal("Amount")) ? 0m : r.GetDecimal(r.GetOrdinal("Amount"))
        };
    }
}

/* -----------------------------------------------------------------
   Lightweight DTOs for full-receipt export (kept here to avoid
   making new files; feel free to move them under Models\ later).
------------------------------------------------------------------*/
namespace HillsCafeManagement.Models
{
    public class ReceiptDetailsModel
    {
        public ReceiptsModel Header { get; set; } = new ReceiptsModel();
        public List<ReceiptLineModel> Lines { get; set; } = new List<ReceiptLineModel>();
        public decimal GrandTotal
        {
            get
            {
                decimal sum = 0m;
                foreach (var l in Lines) sum += l.Subtotal;
                return sum;
            }
        }
    }

    public class ReceiptLineModel
    {
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Subtotal => UnitPrice * Quantity;
    }
}
