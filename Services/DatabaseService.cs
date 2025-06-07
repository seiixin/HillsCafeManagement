using MySql.Data.MySqlClient;
using HillsCafeManagement.Models;
using System;

namespace HillsCafeManagement.Services
{
    public class DatabaseService
    {
        // FIXED: Changed * to _ and fixed string syntax
        private readonly string _connectionString = "server=127.0.0.1;user=root;password=;database=hillscafe_db;";

        public EmployeeModel? Login(string email, string password)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();

                const string query = "SELECT id, fullname, email, password FROM employees WHERE email = @Email AND password = @Password LIMIT 1;";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@Password", password);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return new EmployeeModel
                    {
                        Id = reader.GetInt32("id"),
                        FullName = reader.GetString("fullname"),
                        Email = reader.GetString("email"),
                        Password = reader.GetString("password")
                    };
                }
            }
            catch (MySqlException ex)
            {
                Console.Error.WriteLine($"Database error: {ex.Message}");
                // You might want to show a user-friendly message here
                System.Windows.MessageBox.Show($"Database connection error: {ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Unexpected error: {ex.Message}");
                System.Windows.MessageBox.Show($"An unexpected error occurred: {ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            return null;
        }
    }
}