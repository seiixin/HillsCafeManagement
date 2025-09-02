using HillsCafeManagement.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace HillsCafeManagement.Services
{
    internal class EmployeeService
    {
        private readonly string _connectionString = "server=localhost;user=root;password=;database=hillscafe_db;";

        #region READ

        public List<EmployeeModel> GetAllEmployees()
        {
            var employees = new List<EmployeeModel>();

            try
            {
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
                    var employee = MapEmployee(reader);

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
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error loading employees: " + ex.Message);
            }

            return employees;
        }

        // Optional helper (safe if unused elsewhere)
        public EmployeeModel? GetEmployeeById(int id)
        {
            try
            {
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
                    WHERE e.id = @id
                    LIMIT 1";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@id", id);

                using var reader = cmd.ExecuteReader();
                if (!reader.Read()) return null;

                var employee = MapEmployee(reader);

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

                return employee;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error loading employee: " + ex.Message);
                return null;
            }
        }

        #endregion

        #region DELETE

        public bool DeleteEmployee(int employeeId)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();

                using var transaction = connection.BeginTransaction();

                // Delete linked user account
                using (var deleteUserCmd = new MySqlCommand(
                           "DELETE FROM users WHERE employee_id = @employeeId", connection, transaction))
                {
                    deleteUserCmd.Parameters.AddWithValue("@employeeId", employeeId);
                    deleteUserCmd.ExecuteNonQuery();
                }

                // Delete employee
                int rowsAffected;
                using (var deleteEmployeeCmd = new MySqlCommand(
                           "DELETE FROM employees WHERE id = @id", connection, transaction))
                {
                    deleteEmployeeCmd.Parameters.AddWithValue("@id", employeeId);
                    rowsAffected = deleteEmployeeCmd.ExecuteNonQuery();
                }

                transaction.Commit();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error deleting employee: " + ex.Message);
                return false;
            }
        }

        #endregion

        #region CREATE

        public bool AddEmployee(EmployeeModel employee)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();

                using var transaction = connection.BeginTransaction();

                const string insertEmployeeQuery = @"
                    INSERT INTO employees 
                    (full_name, age, sex, address, birthday, contact_number, position, salary_per_day, shift,
                     sss_number, philhealth_number, pagibig_number, image_url, emergency_contact, date_hired, created_at)
                    VALUES
                    (@FullName, @Age, @Sex, @Address, @Birthday, @ContactNumber, @Position, @SalaryPerDay, @Shift,
                     @SssNumber, @PhilhealthNumber, @PagibigNumber, @ImageUrl, @EmergencyContact, @DateHired, @CreatedAt);
                    SELECT LAST_INSERT_ID();";

                using var cmd = new MySqlCommand(insertEmployeeQuery, connection, transaction);
                cmd.Parameters.AddWithValue("@FullName", employee.FullName ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Age", employee.Age ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Sex", employee.Sex ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Address", employee.Address ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Birthday", employee.Birthday ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@ContactNumber", employee.ContactNumber ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Position", employee.Position ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@SalaryPerDay", employee.SalaryPerDay ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Shift", employee.Shift ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@SssNumber", employee.SssNumber ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@PhilhealthNumber", employee.PhilhealthNumber ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@PagibigNumber", employee.PagibigNumber ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@ImageUrl", employee.ImageUrl ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@EmergencyContact", employee.EmergencyContact ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@DateHired", employee.DateHired ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@CreatedAt", employee.CreatedAt == default ? DateTime.Now : employee.CreatedAt);

                var insertedId = Convert.ToInt32(cmd.ExecuteScalar());
                transaction.Commit();

                employee.Id = insertedId;
                return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error adding employee: " + ex.Message);
                return false;
            }
        }

        #endregion

        #region UPDATE  (KEEP THIS METHOD NAME!)

        // This preserves your original signature so all existing callers build successfully.
        public bool UpdateEmployee(EmployeeModel employee)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();

                const string updateEmployeeQuery = @"
                    UPDATE employees SET
                        full_name         = @FullName,
                        age               = @Age,
                        sex               = @Sex,
                        address           = @Address,
                        birthday          = @Birthday,
                        contact_number    = @ContactNumber,
                        position          = @Position,
                        salary_per_day    = @SalaryPerDay,
                        shift             = @Shift,
                        sss_number        = @SssNumber,
                        philhealth_number = @PhilhealthNumber,
                        pagibig_number    = @PagibigNumber,
                        image_url         = @ImageUrl,
                        emergency_contact = @EmergencyContact,
                        date_hired        = @DateHired
                    WHERE id = @Id";

                using var cmd = new MySqlCommand(updateEmployeeQuery, connection);
                cmd.Parameters.AddWithValue("@FullName", employee.FullName ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Age", employee.Age ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Sex", employee.Sex ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Address", employee.Address ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Birthday", employee.Birthday ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@ContactNumber", employee.ContactNumber ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Position", employee.Position ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@SalaryPerDay", employee.SalaryPerDay ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Shift", employee.Shift ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@SssNumber", employee.SssNumber ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@PhilhealthNumber", employee.PhilhealthNumber ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@PagibigNumber", employee.PagibigNumber ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@ImageUrl", employee.ImageUrl ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@EmergencyContact", employee.EmergencyContact ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@DateHired", employee.DateHired ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Id", employee.Id);

                int rowsAffected = cmd.ExecuteNonQuery();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error updating employee: " + ex.Message);
                return false;
            }
        }

        // Optional convenience if you want to update ONLY the photo from some UI
        public bool UpdateEmployeeImage(int employeeId, string imageUrl)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();

                const string sql = @"UPDATE employees SET image_url = @ImageUrl WHERE id = @Id;";
                using var cmd = new MySqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@ImageUrl", (object?)imageUrl ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Id", employeeId);
                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error updating employee image: " + ex.Message);
                return false;
            }
        }

        #endregion

        #region mapper

        private static EmployeeModel MapEmployee(MySqlDataReader reader) => new EmployeeModel
        {
            Id = reader.GetInt32("id"),
            FullName = reader["full_name"]?.ToString(),
            Age = reader.IsDBNull(reader.GetOrdinal("age")) ? (int?)null : reader.GetInt32("age"),
            Sex = reader["sex"]?.ToString(),
            Address = reader["address"]?.ToString(),
            Birthday = reader.IsDBNull(reader.GetOrdinal("birthday")) ? (DateTime?)null : reader.GetDateTime("birthday"),
            ContactNumber = reader["contact_number"]?.ToString(),
            Position = reader["position"]?.ToString(),
            SalaryPerDay = reader.IsDBNull(reader.GetOrdinal("salary_per_day")) ? (decimal?)null : reader.GetDecimal("salary_per_day"),
            Shift = reader["shift"]?.ToString(),
            SssNumber = reader["sss_number"]?.ToString(),
            PhilhealthNumber = reader["philhealth_number"]?.ToString(),
            PagibigNumber = reader["pagibig_number"]?.ToString(),
            ImageUrl = reader["image_url"]?.ToString(),
            EmergencyContact = reader["emergency_contact"]?.ToString(),
            DateHired = reader.IsDBNull(reader.GetOrdinal("date_hired")) ? (DateTime?)null : reader.GetDateTime("date_hired"),
            CreatedAt = reader.IsDBNull(reader.GetOrdinal("created_at")) ? DateTime.Now : reader.GetDateTime("created_at")
        };

        #endregion
    }
}
