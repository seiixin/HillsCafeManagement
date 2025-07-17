using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using HillsCafeManagement.Models;
using HillsCafeManagement.Helpers; // <-- Make sure RelayCommand is in this namespace

namespace HillsCafeManagement.ViewModels
{
    public class PayrollViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<PayrollModel> PayrollList { get; set; } = new();

        private ObservableCollection<PayrollModel> _allPayrolls = new();

        public ICommand DeleteCommand { get; }

        public PayrollViewModel()
        {
            DeleteCommand = new RelayCommand<PayrollModel>(DeletePayroll);
        }

        // Load payrolls into the ViewModel
        public void LoadPayrolls(List<PayrollModel> payrolls)
        {
            _allPayrolls = new ObservableCollection<PayrollModel>(payrolls);
            PayrollList.Clear();
            foreach (var p in _allPayrolls)
            {
                PayrollList.Add(p);
            }
            OnPropertyChanged(nameof(PayrollList));
        }

        // Filter payroll list based on search term
        public void FilterPayroll(string filter)
        {
            if (string.IsNullOrEmpty(filter))
            {
                PayrollList.Clear();
                foreach (var p in _allPayrolls)
                {
                    PayrollList.Add(p);
                }
            }
            else
            {
                filter = filter.ToLower();

                var filtered = _allPayrolls.Where(p =>
                    p.EmployeeId.ToString().Contains(filter) ||
                    (!string.IsNullOrEmpty(p.BranchName) && p.BranchName.ToLower().Contains(filter)) ||
                    (!string.IsNullOrEmpty(p.ShiftType) && p.ShiftType.ToLower().Contains(filter)) ||
                    p.StartDate.ToString("d").Contains(filter) ||
                    p.EndDate.ToString("d").Contains(filter)
                ).ToList();

                PayrollList.Clear();
                foreach (var p in filtered)
                {
                    PayrollList.Add(p);
                }
            }
            OnPropertyChanged(nameof(PayrollList));
        }

        private void DeletePayroll(PayrollModel? payroll)
        {
            if (payroll == null) return;

            var confirm = MessageBox.Show(
                $"Are you sure you want to delete payroll for employee #{payroll.EmployeeId}?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            // Remove from both lists
            _allPayrolls.Remove(payroll);
            PayrollList.Remove(payroll);

            // Optional: Call service to remove from database
            // _payrollService.DeletePayrollById(payroll.Id);

            OnPropertyChanged(nameof(PayrollList));
        }

        // Implement INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
