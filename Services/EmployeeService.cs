using HillsCafeManagement.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                        CreatedAt = reader.GetDateTime("created_at")
                    };

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
                var deleteUserCmd = new MySqlCommand("DELETE FROM users WHERE employee_id = @employeeId", connection, transaction);
                deleteUserCmd.Parameters.AddWithValue("@employeeId", employeeId);
                deleteUserCmd.ExecuteNonQuery();

                // Delete employee
                var deleteEmployeeCmd = new MySqlCommand("DELETE FROM employees WHERE id = @id", connection, transaction);
                deleteEmployeeCmd.Parameters.AddWithValue("@id", employeeId);
                int rowsAffected = deleteEmployeeCmd.ExecuteNonQuery();

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

        #region CREATE (AddEmployee)

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
                cmd.Parameters.AddWithValue("@FullName", employee.FullName);
                cmd.Parameters.AddWithValue("@Age", employee.Age ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Sex", employee.Sex);
                cmd.Parameters.AddWithValue("@Address", employee.Address);
                cmd.Parameters.AddWithValue("@Birthday", employee.Birthday ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@ContactNumber", employee.ContactNumber);
                cmd.Parameters.AddWithValue("@Position", employee.Position);
                cmd.Parameters.AddWithValue("@SalaryPerDay", employee.SalaryPerDay ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Shift", employee.Shift);
                cmd.Parameters.AddWithValue("@SssNumber", employee.SssNumber);
                cmd.Parameters.AddWithValue("@PhilhealthNumber", employee.PhilhealthNumber);
                cmd.Parameters.AddWithValue("@PagibigNumber", employee.PagibigNumber);
                cmd.Parameters.AddWithValue("@ImageUrl", employee.ImageUrl);
                cmd.Parameters.AddWithValue("@EmergencyContact", employee.EmergencyContact);
                cmd.Parameters.AddWithValue("@DateHired", employee.DateHired ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@CreatedAt", employee.CreatedAt);

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

        #region UPDATE (EditEmployee)

        public bool UpdateEmployee(EmployeeModel employee)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();

                const string updateEmployeeQuery = @"
            UPDATE employees SET
                full_name = @FullName,
                age = @Age,
                sex = @Sex,
                address = @Address,
                birthday = @Birthday,
                contact_number = @ContactNumber,
                position = @Position,
                salary_per_day = @SalaryPerDay,
                shift = @Shift,
                sss_number = @SssNumber,
                philhealth_number = @PhilhealthNumber,
                pagibig_number = @PagibigNumber,
                image_url = @ImageUrl,
                emergency_contact = @EmergencyContact,
                date_hired = @DateHired
            WHERE id = @Id";

                using var cmd = new MySqlCommand(updateEmployeeQuery, connection);
                cmd.Parameters.AddWithValue("@FullName", employee.FullName);
                cmd.Parameters.AddWithValue("@Age", employee.Age ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Sex", employee.Sex);
                cmd.Parameters.AddWithValue("@Address", employee.Address);
                cmd.Parameters.AddWithValue("@Birthday", employee.Birthday ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@ContactNumber", employee.ContactNumber);
                cmd.Parameters.AddWithValue("@Position", employee.Position);
                cmd.Parameters.AddWithValue("@SalaryPerDay", employee.SalaryPerDay ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Shift", employee.Shift);
                cmd.Parameters.AddWithValue("@SssNumber", employee.SssNumber);
                cmd.Parameters.AddWithValue("@PhilhealthNumber", employee.PhilhealthNumber);
                cmd.Parameters.AddWithValue("@PagibigNumber", employee.PagibigNumber);
                cmd.Parameters.AddWithValue("@ImageUrl", employee.ImageUrl);
                cmd.Parameters.AddWithValue("@EmergencyContact", employee.EmergencyContact);
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

        #endregion


    }
}
