using MySql.Data.MySqlClient;
using HillsCafeManagement.Models;
using System;
using System.Collections.Generic;

namespace HillsCafeManagement.Services
{
    public class DatabaseService
    {
        // ✅ Make sure only ONE _connectionString is defined
        private readonly string _connectionString = "server=localhost;user=root;password=;database=hillscafe_db;";

        // ✅ Login using EMAIL (correct for your current UserModel)
        public UserModel? AuthenticateUser(string email, string password)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();

                const string query = @"
                    SELECT 
                        u.id AS user_id,
                        u.email,
                        u.password,
                        u.role,
                        u.employee_id,
                        e.full_name,
                        e.position
                    FROM users u
                    LEFT JOIN employees e ON u.employee_id = e.id
                    WHERE u.email = @Email AND u.password = @Password
                    LIMIT 1;";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@Password", password);

                using var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    EmployeeModel? employee = null;

                    if (!reader.IsDBNull(reader.GetOrdinal("employee_id")))
                    {
                        employee = new EmployeeModel
                        {
                            Id = reader.GetInt32("employee_id"),
                            FullName = reader.IsDBNull(reader.GetOrdinal("full_name")) ? "" : reader.GetString("full_name"),
                            Position = reader.IsDBNull(reader.GetOrdinal("position")) ? "" : reader.GetString("position")
                        };
                    }

                    return new UserModel
                    {
                        Id = reader.GetInt32("user_id"),
                        Email = reader.GetString("email"),
                        Password = reader.GetString("password"),
                        Role = reader.GetString("role"),
                        EmployeeId = reader.IsDBNull(reader.GetOrdinal("employee_id")) ? null : reader.GetInt32("employee_id"),
                        Employee = employee
                    };
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Login error: " + ex.Message);
            }

            return null;
        }

        // ✅ Gets all users with their linked employee
        public List<UserModel> GetAllUsers()
        {
            var users = new List<UserModel>();

            using var connection = new MySqlConnection(_connectionString);
            connection.Open();

            const string query = @"
                SELECT 
                    u.id,
                    u.email,
                    u.password,
                    u.role,
                    u.employee_id,
                    e.full_name
                FROM users u
                LEFT JOIN employees e ON u.employee_id = e.id";

            using var cmd = new MySqlCommand(query, connection);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var user = new UserModel
                {
                    Id = reader.GetInt32("id"),
                    Email = reader.GetString("email"),
                    Password = reader.GetString("password"),
                    Role = reader.GetString("role"),
                    EmployeeId = reader.IsDBNull(reader.GetOrdinal("employee_id")) ? null : reader.GetInt32("employee_id"),
                    Employee = new EmployeeModel
                    {
                        FullName = reader.IsDBNull(reader.GetOrdinal("full_name")) ? "" : reader.GetString("full_name")
                    }
                };

                users.Add(user);
            }

            return users;
        }

        // ✅ Delete user by ID
        public bool DeleteUserById(int id)
        {
            using var connection = new MySqlConnection(_connectionString);
            connection.Open();

            using var cmd = new MySqlCommand("DELETE FROM users WHERE id = @id", connection);
            cmd.Parameters.AddWithValue("@id", id);

            return cmd.ExecuteNonQuery() > 0;
        }

        public List<EmployeeModel> GetAllEmployees()
        {
            var employees = new List<EmployeeModel>();

            using var connection = new MySqlConnection(_connectionString);
            connection.Open();

            const string query = @"
        SELECT 
            e.*, 
            u.id AS user_id, 
            u.email, 
            u.role
        FROM employees e
        LEFT JOIN users u ON u.employee_id = e.id
        ORDER BY e.id DESC";

            using var cmd = new MySqlCommand(query, connection);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var employee = new EmployeeModel
                {
                    Id = reader.GetInt32("id"),
                    FullName = reader["full_name"]?.ToString(),
                    Age = reader["age"] as int?,
                    Sex = reader["sex"]?.ToString(),
                    Address = reader["address"]?.ToString(),
                    Birthday = reader["birthday"] as DateTime?,
                    ContactNumber = reader["contact_number"]?.ToString(),
                    Position = reader["position"]?.ToString(),
                    SalaryPerDay = reader["salary_per_day"] as decimal?,
                    Shift = reader["shift"]?.ToString(),
                    SssNumber = reader["sss_number"]?.ToString(),
                    PhilhealthNumber = reader["philhealth_number"]?.ToString(),
                    PagibigNumber = reader["pagibig_number"]?.ToString(),
                    ImageUrl = reader["image_url"]?.ToString(),
                    EmergencyContact = reader["emergency_contact"]?.ToString(),
                    DateHired = reader["date_hired"] as DateTime?,
                    CreatedAt = reader.GetDateTime("created_at"),
                };

                // Optional: Load linked user if available
                if (!reader.IsDBNull(reader.GetOrdinal("user_id")))
                {
                    employee.UserAccount = new UserModel
                    {
                        Id = reader.GetInt32("user_id"),
                        Email = reader["email"]?.ToString(),
                        Role = reader["role"]?.ToString(),
                        EmployeeId = employee.Id
                    };
                }

                employees.Add(employee);
            }

            return employees;
        }
    }
}
