using System;
using System.Collections.ObjectModel;
using System.Linq;
using HillsCafeManagement.Models;
using HillsCafeManagement.Services;

namespace HillsCafeManagement.ViewModels
{
    public class AdminPayslipViewModel : BaseViewModel
    {
        public ObservableCollection<PayslipRequestModel> PayslipRequests { get; set; } = new();
        private ObservableCollection<PayslipRequestModel> _allRequests = new();

        public AdminPayslipViewModel()
        {
            LoadPayslipRequestsFromDatabase();
        }

        public void LoadPayslipRequestsFromDatabase()
        {
            var service = new PayslipService();
            var data = service.GetAllPayslipRequests();
            _allRequests = new ObservableCollection<PayslipRequestModel>(data);
            PayslipRequests = new ObservableCollection<PayslipRequestModel>(_allRequests);
            OnPropertyChanged(nameof(PayslipRequests));
        }

        public void FilterPayslipRequests(string filterText)
        {
            if (string.IsNullOrWhiteSpace(filterText))
            {
                PayslipRequests = new ObservableCollection<PayslipRequestModel>(_allRequests);
            }
            else
            {
                PayslipRequests = new ObservableCollection<PayslipRequestModel>(
                    _allRequests.Where(p =>
                        p.EmployeeId.ToString().Contains(filterText) ||
                        (!string.IsNullOrWhiteSpace(p.FullName) && p.FullName.Contains(filterText, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrWhiteSpace(p.Status) && p.Status.Contains(filterText, StringComparison.OrdinalIgnoreCase))));
            }
            OnPropertyChanged(nameof(PayslipRequests));
        }
    }
}
