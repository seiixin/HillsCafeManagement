﻿using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Data;
using MySql.Data.MySqlClient;
using HillsCafeManagement.Models;

namespace HillsCafeManagement.Services
{
    public class InventoryService
    {
        private readonly string connectionString = "server=localhost;user=root;password=;database=hillscafe_db;";

        public ObservableCollection<InventoryItem> GetAllItems()
        {
            var items = new ObservableCollection<InventoryItem>();

            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = @"SELECT Id, ProductName, CategoryName, Quantity, ExpiryDate 
                                FROM inventory_items ORDER BY ProductName";

                using (var command = new MySqlCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(new InventoryItem
                        {
                            Id = reader.GetInt32("Id"),
                            ProductName = reader.GetString("ProductName"),
                            CategoryName = reader.IsDBNull("CategoryName") ? null : reader.GetString("CategoryName"),
                            Quantity = reader.GetInt32("Quantity"),
                            ExpiryDate = reader.IsDBNull("ExpiryDate") ? null : reader.GetDateTime("ExpiryDate")
                        });
                    }
                }
            }

            return items;
        }

        public InventoryItem GetItemById(int id)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = @"SELECT Id, ProductName, CategoryName, Quantity, ExpiryDate 
                                FROM inventory_items WHERE Id = @Id";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new InventoryItem
                            {
                                Id = reader.GetInt32("Id"),
                                ProductName = reader.GetString("ProductName"),
                                CategoryName = reader.IsDBNull("CategoryName") ? null : reader.GetString("CategoryName"),
                                Quantity = reader.GetInt32("Quantity"),
                                ExpiryDate = reader.IsDBNull("ExpiryDate") ? null : reader.GetDateTime("ExpiryDate")
                            };
                        }
                    }
                }
            }
            return null;
        }

        public bool AddItem(InventoryItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"INSERT INTO inventory_items (ProductName, CategoryName, Quantity, ExpiryDate) 
                                    VALUES (@ProductName, @CategoryName, @Quantity, @ExpiryDate)";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ProductName", item.ProductName);
                        command.Parameters.AddWithValue("@CategoryName", item.CategoryName ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Quantity", item.Quantity);
                        command.Parameters.AddWithValue("@ExpiryDate", item.ExpiryDate ?? (object)DBNull.Value);

                        int result = command.ExecuteNonQuery();
                        return result > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error or handle as needed
                System.Diagnostics.Debug.WriteLine($"Error adding item: {ex.Message}");
                return false;
            }
        }

        public bool UpdateItem(InventoryItem updatedItem)
        {
            if (updatedItem == null) throw new ArgumentNullException(nameof(updatedItem));

            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"UPDATE inventory_items 
                                    SET ProductName = @ProductName, 
                                        CategoryName = @CategoryName, 
                                        Quantity = @Quantity, 
                                        ExpiryDate = @ExpiryDate 
                                    WHERE Id = @Id";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", updatedItem.Id);
                        command.Parameters.AddWithValue("@ProductName", updatedItem.ProductName);
                        command.Parameters.AddWithValue("@CategoryName", updatedItem.CategoryName ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Quantity", updatedItem.Quantity);
                        command.Parameters.AddWithValue("@ExpiryDate", updatedItem.ExpiryDate ?? (object)DBNull.Value);

                        int result = command.ExecuteNonQuery();
                        return result > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error or handle as needed
                System.Diagnostics.Debug.WriteLine($"Error updating item: {ex.Message}");
                return false;
            }
        }

        public bool DeleteItem(int id)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "DELETE FROM inventory_items WHERE Id = @Id";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        int result = command.ExecuteNonQuery();
                        return result > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error or handle as needed
                System.Diagnostics.Debug.WriteLine($"Error deleting item: {ex.Message}");
                return false;
            }
        }

        public ObservableCollection<InventoryItem> SearchItems(string searchText)
        {
            var items = new ObservableCollection<InventoryItem>();

            if (string.IsNullOrWhiteSpace(searchText))
                return GetAllItems();

            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = @"SELECT Id, ProductName, CategoryName, Quantity, ExpiryDate 
                                FROM inventory_items 
                                WHERE ProductName LIKE @SearchText 
                                   OR CategoryName LIKE @SearchText 
                                ORDER BY ProductName";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@SearchText", $"%{searchText}%");
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            items.Add(new InventoryItem
                            {
                                Id = reader.GetInt32("Id"),
                                ProductName = reader.GetString("ProductName"),
                                CategoryName = reader.IsDBNull("CategoryName") ? null : reader.GetString("CategoryName"),
                                Quantity = reader.GetInt32("Quantity"),
                                ExpiryDate = reader.IsDBNull("ExpiryDate") ? null : reader.GetDateTime("ExpiryDate")
                            });
                        }
                    }
                }
            }

            return items;
        }

        public ObservableCollection<InventoryItem> GetExpiringItems()
        {
            var items = new ObservableCollection<InventoryItem>();

            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = @"SELECT Id, ProductName, CategoryName, Quantity, ExpiryDate 
                                FROM inventory_items 
                                WHERE ExpiryDate IS NOT NULL 
                                  AND ExpiryDate <= DATE_ADD(CURDATE(), INTERVAL 7 DAY)
                                ORDER BY ExpiryDate";

                using (var command = new MySqlCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(new InventoryItem
                        {
                            Id = reader.GetInt32("Id"),
                            ProductName = reader.GetString("ProductName"),
                            CategoryName = reader.IsDBNull("CategoryName") ? null : reader.GetString("CategoryName"),
                            Quantity = reader.GetInt32("Quantity"),
                            ExpiryDate = reader.GetDateTime("ExpiryDate")
                        });
                    }
                }
            }

            return items;
        }

        public ObservableCollection<InventoryItem> GetLowStockItems(int threshold = 10)
        {
            var items = new ObservableCollection<InventoryItem>();

            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = @"SELECT Id, ProductName, CategoryName, Quantity, ExpiryDate 
                                FROM inventory_items 
                                WHERE Quantity <= @Threshold 
                                ORDER BY Quantity";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Threshold", threshold);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            items.Add(new InventoryItem
                            {
                                Id = reader.GetInt32("Id"),
                                ProductName = reader.GetString("ProductName"),
                                CategoryName = reader.IsDBNull("CategoryName") ? null : reader.GetString("CategoryName"),
                                Quantity = reader.GetInt32("Quantity"),
                                ExpiryDate = reader.IsDBNull("ExpiryDate") ? null : reader.GetDateTime("ExpiryDate")
                            });
                        }
                    }
                }
            }

            return items;
        }

        public void InitializeDatabase()
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // Create table if it doesn't exist
                    string createTableQuery = @"
                        CREATE TABLE IF NOT EXISTS inventory_items (
                            Id INT AUTO_INCREMENT PRIMARY KEY,
                            ProductName VARCHAR(255) NOT NULL,
                            CategoryName VARCHAR(255),
                            Quantity INT NOT NULL DEFAULT 0,
                            ExpiryDate DATE,
                            CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                            UpdatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
                        )";

                    using (var command = new MySqlCommand(createTableQuery, connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    // Check if table is empty and add sample data
                    string countQuery = "SELECT COUNT(*) FROM inventory_items";
                    using (var countCommand = new MySqlCommand(countQuery, connection))
                    {
                        int count = Convert.ToInt32(countCommand.ExecuteScalar());
                        if (count == 0)
                        {
                            InsertSampleData(connection);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing database: {ex.Message}");
            }
        }

        private void InsertSampleData(MySqlConnection connection)
        {
            string insertQuery = @"
                INSERT INTO inventory_items (ProductName, CategoryName, Quantity, ExpiryDate) VALUES
                ('Coffee Beans - Arabica', 'Beverages', 25, DATE_ADD(CURDATE(), INTERVAL 30 DAY)),
                ('Milk - Whole', 'Dairy', 8, DATE_ADD(CURDATE(), INTERVAL 3 DAY)),
                ('Sugar - White', 'Sweeteners', 50, NULL),
                ('Croissants - Frozen', 'Bakery', 15, DATE_ADD(CURDATE(), INTERVAL 5 DAY)),
                ('Cheese - Cheddar', 'Dairy', 3, DATE_ADD(CURDATE(), INTERVAL 1 DAY))";

            using (var command = new MySqlCommand(insertQuery, connection))
            {
                command.ExecuteNonQuery();
            }
        }
    }
}