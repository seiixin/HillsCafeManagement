using HillsCafeManagement.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace HillsCafeManagement.Services
{
    public interface IEmployeeService
    {
        List<EmployeeModel> GetAllEmployees();
        EmployeeModel? GetEmployeeById(int id);
        bool UpdateEmployee(EmployeeModel employee);
        bool UpdateEmployeeImage(int employeeId, string imageUrl);
        bool AddEmployee(EmployeeModel employee);
        bool DeleteEmployee(int employeeId);
    }

    public sealed class EmployeeService : IEmployeeService
    {
        private readonly string _connectionString;

        // Allow DI / config to provide the connection string; default to your current literal.
        public EmployeeService(string? connectionString = null)
        {
            _connectionString = string.IsNullOrWhiteSpace(connectionString)
                ? "server=localhost;user=root;password=;database=hillscafe_db;"
                : connectionString!;
        }

        // =====================================================================
        // READ
        // =====================================================================

        public List<EmployeeModel> GetAllEmployees()
        {
            var employees = new List<EmployeeModel>();

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();

                const string sql = @"
                    SELECT 
                        e.*,
                        u.id    AS user_id,
                        u.email AS email,
                        u.role  AS role
                    FROM employees e
                    LEFT JOIN users u ON u.employee_id = e.id
                    ORDER BY e.id DESC;";

                using var cmd = new MySqlCommand(sql, connection);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    var emp = MapEmployee(reader);

                    if (!reader.IsDBNull(reader.GetOrdinal("user_id")))
                    {
                        emp.UserAccount = new UserModel
                        {
                            Id = reader.GetInt32("user_id"),
                            Email = reader["email"]?.ToString(),
                            Role = reader["role"]?.ToString(),
                            EmployeeId = emp.Id
                        };
                    }

                    employees.Add(emp);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error loading employees: " + ex.Message);
            }

            return employees;
        }

        public EmployeeModel? GetEmployeeById(int id)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();

                const string sql = @"
                    SELECT 
                        e.*,
                        u.id    AS user_id,
                        u.email AS email,
                        u.role  AS role
                    FROM employees e
                    LEFT JOIN users u ON u.employee_id = e.id
                    WHERE e.id = @id
                    LIMIT 1;";

                using var cmd = new MySqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@id", id);

                using var reader = cmd.ExecuteReader();
                if (!reader.Read()) return null;

                var emp = MapEmployee(reader);

                if (!reader.IsDBNull(reader.GetOrdinal("user_id")))
                {
                    emp.UserAccount = new UserModel
                    {
                        Id = reader.GetInt32("user_id"),
                        Email = reader["email"]?.ToString(),
                        Role = reader["role"]?.ToString(),
                        EmployeeId = emp.Id
                    };
                }

                return emp;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error loading employee: " + ex.Message);
                return null;
            }
        }

        // =====================================================================
        // CREATE
        // =====================================================================

        public bool AddEmployee(EmployeeModel employee)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();
                using var tx = connection.BeginTransaction();

                const string sql = @"
                    INSERT INTO employees 
                    (full_name, age, sex, address, birthday, contact_number, position, salary_per_day, shift,
                     sss_number, philhealth_number, pagibig_number, image_url, emergency_contact, date_hired, created_at)
                    VALUES
                    (@FullName, @Age, @Sex, @Address, @Birthday, @ContactNumber, @Position, @SalaryPerDay, @Shift,
                     @SssNumber, @PhilhealthNumber, @PagibigNumber, @ImageUrl, @EmergencyContact, @DateHired, @CreatedAt);
                    SELECT LAST_INSERT_ID();";

                using var cmd = new MySqlCommand(sql, connection, tx);
                cmd.Parameters.AddWithValue("@FullName", ParamOrDbNull(employee.FullName));
                cmd.Parameters.AddWithValue("@Age", ParamOrDbNull(employee.Age));
                cmd.Parameters.AddWithValue("@Sex", ParamOrDbNull(employee.Sex));
                cmd.Parameters.AddWithValue("@Address", ParamOrDbNull(employee.Address));
                cmd.Parameters.AddWithValue("@Birthday", ParamOrDbNull(employee.Birthday));
                cmd.Parameters.AddWithValue("@ContactNumber", ParamOrDbNull(employee.ContactNumber));
                cmd.Parameters.AddWithValue("@Position", ParamOrDbNull(employee.Position));
                cmd.Parameters.AddWithValue("@SalaryPerDay", ParamOrDbNull(employee.SalaryPerDay));
                cmd.Parameters.AddWithValue("@Shift", ParamOrDbNull(employee.Shift));
                cmd.Parameters.AddWithValue("@SssNumber", ParamOrDbNull(employee.SssNumber));
                cmd.Parameters.AddWithValue("@PhilhealthNumber", ParamOrDbNull(employee.PhilhealthNumber));
                cmd.Parameters.AddWithValue("@PagibigNumber", ParamOrDbNull(employee.PagibigNumber));
                cmd.Parameters.AddWithValue("@ImageUrl", ParamOrDbNull(employee.ImageUrl));
                cmd.Parameters.AddWithValue("@EmergencyContact", ParamOrDbNull(employee.EmergencyContact));
                cmd.Parameters.AddWithValue("@DateHired", ParamOrDbNull(employee.DateHired));
                cmd.Parameters.AddWithValue("@CreatedAt", employee.CreatedAt == default ? DateTime.Now : employee.CreatedAt);

                var newIdObj = cmd.ExecuteScalar();
                tx.Commit();

                var newId = Convert.ToInt32(newIdObj);
                employee.Id = newId;
                return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error adding employee: " + ex.Message);
                return false;
            }
        }

        // =====================================================================
        // UPDATE
        // =====================================================================

        // Keep this exact name/signature to satisfy existing callers
        public bool UpdateEmployee(EmployeeModel employee)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();

                const string sql = @"
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
                    WHERE id = @Id;";

                using var cmd = new MySqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@FullName", ParamOrDbNull(employee.FullName));
                cmd.Parameters.AddWithValue("@Age", ParamOrDbNull(employee.Age));
                cmd.Parameters.AddWithValue("@Sex", ParamOrDbNull(employee.Sex));
                cmd.Parameters.AddWithValue("@Address", ParamOrDbNull(employee.Address));
                cmd.Parameters.AddWithValue("@Birthday", ParamOrDbNull(employee.Birthday));
                cmd.Parameters.AddWithValue("@ContactNumber", ParamOrDbNull(employee.ContactNumber));
                cmd.Parameters.AddWithValue("@Position", ParamOrDbNull(employee.Position));
                cmd.Parameters.AddWithValue("@SalaryPerDay", ParamOrDbNull(employee.SalaryPerDay));
                cmd.Parameters.AddWithValue("@Shift", ParamOrDbNull(employee.Shift));
                cmd.Parameters.AddWithValue("@SssNumber", ParamOrDbNull(employee.SssNumber));
                cmd.Parameters.AddWithValue("@PhilhealthNumber", ParamOrDbNull(employee.PhilhealthNumber));
                cmd.Parameters.AddWithValue("@PagibigNumber", ParamOrDbNull(employee.PagibigNumber));
                cmd.Parameters.AddWithValue("@ImageUrl", ParamOrDbNull(employee.ImageUrl));
                cmd.Parameters.AddWithValue("@EmergencyContact", ParamOrDbNull(employee.EmergencyContact));
                cmd.Parameters.AddWithValue("@DateHired", ParamOrDbNull(employee.DateHired));
                cmd.Parameters.AddWithValue("@Id", employee.Id);

                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error updating employee: " + ex.Message);
                return false;
            }
        }

        public bool UpdateEmployeeImage(int employeeId, string imageUrl)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();

                const string sql = "UPDATE employees SET image_url = @ImageUrl WHERE id = @Id;";
                using var cmd = new MySqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@ImageUrl", ParamOrDbNull(imageUrl));
                cmd.Parameters.AddWithValue("@Id", employeeId);

                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error updating employee image: " + ex.Message);
                return false;
            }
        }

        // =====================================================================
        // DELETE
        // =====================================================================

        public bool DeleteEmployee(int employeeId)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();
                using var tx = connection.BeginTransaction();

                using (var delUser = new MySqlCommand("DELETE FROM users WHERE employee_id = @eid;", connection, tx))
                {
                    delUser.Parameters.AddWithValue("@eid", employeeId);
                    delUser.ExecuteNonQuery();
                }

                int rows;
                using (var delEmp = new MySqlCommand("DELETE FROM employees WHERE id = @id;", connection, tx))
                {
                    delEmp.Parameters.AddWithValue("@id", employeeId);
                    rows = delEmp.ExecuteNonQuery();
                }

                tx.Commit();
                return rows > 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error deleting employee: " + ex.Message);
                return false;
            }
        }

        // =====================================================================
        // Mapper
        // =====================================================================

        private static EmployeeModel MapEmployee(MySqlDataReader r) => new EmployeeModel
        {
            Id = r.GetInt32("id"),
            FullName = r["full_name"]?.ToString(),
            Age = r.IsDBNull(r.GetOrdinal("age")) ? (int?)null : r.GetInt32("age"),
            Sex = r["sex"]?.ToString(),
            Address = r["address"]?.ToString(),
            Birthday = r.IsDBNull(r.GetOrdinal("birthday")) ? (DateTime?)null : r.GetDateTime("birthday"),
            ContactNumber = r["contact_number"]?.ToString(),
            Position = r["position"]?.ToString(),
            SalaryPerDay = r.IsDBNull(r.GetOrdinal("salary_per_day")) ? (decimal?)null : r.GetDecimal("salary_per_day"),
            Shift = r["shift"]?.ToString(),
            SssNumber = r["sss_number"]?.ToString(),
            PhilhealthNumber = r["philhealth_number"]?.ToString(),
            PagibigNumber = r["pagibig_number"]?.ToString(),
            ImageUrl = r["image_url"]?.ToString(),
            EmergencyContact = r["emergency_contact"]?.ToString(),
            DateHired = r.IsDBNull(r.GetOrdinal("date_hired")) ? (DateTime?)null : r.GetDateTime("date_hired"),
            CreatedAt = r.IsDBNull(r.GetOrdinal("created_at")) ? DateTime.Now : r.GetDateTime("created_at")
        };

        private static object ParamOrDbNull(object? value) => value ?? DBNull.Value;
    }
}
