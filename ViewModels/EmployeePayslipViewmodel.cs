using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.CompilerServices;
using HillsCafeManagement.Models;
using HillsCafeManagement.Services;

namespace HillsCafeManagement.ViewModels
{
    public class EmployeePayslipViewModel : BaseViewModel
    {
        public ObservableCollection<PayslipModel> Payslips { get; set; } = new();
        private ObservableCollection<PayslipModel> _allPayslips = new();

        public void LoadPayslipsFromDatabase()
        {
            PayslipService service = new PayslipService();
            var data = service.GetEmployeePayslips(1); // Replace 1 with actual employee ID
            _allPayslips = new ObservableCollection<PayslipModel>(data);
            Payslips = new ObservableCollection<PayslipModel>(_allPayslips);
            OnPropertyChanged(nameof(Payslips));
        }

        public void FilterPayslips(string filterText)
        {
            if (string.IsNullOrWhiteSpace(filterText))
            {
                Payslips = new ObservableCollection<PayslipModel>(_allPayslips);
            }
            else
            {
                Payslips = new ObservableCollection<PayslipModel>(
                    _allPayslips.Where(p =>
                        p.PayDate.ToString("d").Contains(filterText, StringComparison.OrdinalIgnoreCase) ||
                        p.HoursWorked.ToString().Contains(filterText) ||
                        p.NetSalary.ToString().Contains(filterText)));
            }
            OnPropertyChanged(nameof(Payslips));
        }
    }
}