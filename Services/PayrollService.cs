using System;
using System.Collections.Generic;
using System.Linq;
using HillsCafeManagement.Helpers;
using HillsCafeManagement.Models;

namespace HillsCafeManagement.Services
{
    public class PayrollService
    {
        public PayrollModel GeneratePayroll(EmployeeModel employee, DateTime startDate, DateTime endDate, List<AttendanceModel> attendanceRecords)
        {
            var daysWorked = PayrollCalculationHelper.GetDaysWorked(startDate, endDate, attendanceRecords);

            var dailyRate = employee.SalaryPerDay ?? 0;
            var grossSalary = PayrollCalculationHelper.CalculateGrossSalary(daysWorked, dailyRate);

            decimal sss = 500;         
            decimal philhealth = 300;  
            decimal pagibig = 200;     
            decimal other = 100;       

            var totalDeductions = PayrollCalculationHelper.CalculateTotalDeductions(sss, philhealth, pagibig, other);
            var netSalary = PayrollCalculationHelper.CalculateNetSalary(grossSalary, totalDeductions);

            return new PayrollModel
            {
                EmployeeId = employee.Id.ToString(),
                FullName = employee.FullName ?? string.Empty,
                PayDate = endDate.ToShortDateString(),
                HoursWorked = daysWorked * 8, // 8 hours/day
                RatePerHour = dailyRate / 8,  // 8 hours/day
                Deductions = totalDeductions,
                NetSalary = netSalary
            };
        }
    }
}
