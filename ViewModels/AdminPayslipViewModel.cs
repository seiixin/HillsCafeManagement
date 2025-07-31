using HillsCafeManagement.Models;
using HillsCafeManagement.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace HillsCafeManagement.ViewModels
{
    public class AdminPayslipViewModel : BaseViewModel
    {
        private ObservableCollection<PayslipRequestModel> _allRequests = new();
        private readonly PayslipService _payslipService;

        public ObservableCollection<PayslipRequestModel> PayslipRequests { get; set; } = new();

        public ICommand ApproveCommand { get; }
        public ICommand DenyCommand { get; }
        public ICommand DoneCommand { get; }

        public AdminPayslipViewModel()
        {
            _payslipService = new PayslipService();

            ApproveCommand = new RelayCommand<PayslipRequestModel>(ApproveRequest);
            DenyCommand = new RelayCommand<PayslipRequestModel>(DenyRequest);
            DoneCommand = new RelayCommand<PayslipRequestModel>(MarkRequestDone);

            LoadPayslipRequestsFromDatabase();
        }

        public void LoadPayslipRequestsFromDatabase()
        {
            var data = _payslipService.GetAllPayslipRequests();
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

        private void ApproveRequest(PayslipRequestModel request)
        {
            if (request == null) return;
            request.Status = "Approved";
            _payslipService.UpdateRequestStatus(request.Id, "Approved");
            OnPropertyChanged(nameof(PayslipRequests));
        }

        private void DenyRequest(PayslipRequestModel request)
        {
            if (request == null) return;
            request.Status = "Denied";
            _payslipService.UpdateRequestStatus(request.Id, "Denied");
            OnPropertyChanged(nameof(PayslipRequests));
        }

        private void MarkRequestDone(PayslipRequestModel request)
        {
            if (request == null) return;
            request.Status = "Done";
            _payslipService.UpdateRequestStatus(request.Id, "Done");
            OnPropertyChanged(nameof(PayslipRequests));
        }
    }
}
