using HillsCafeManagement.Helpers;
using HillsCafeManagement.Models;
using HillsCafeManagement.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace HillsCafeManagement.ViewModels
{
    public class AttendanceAdminViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<AttendanceModel> Attendances { get; set; } = new();

        private DateTime? _filterDate;
        public DateTime? FilterDate
        {
            get => _filterDate;
            set { _filterDate = value; OnPropertyChanged(nameof(FilterDate)); }
        }

        private string _filterEmployeeId;
        public string FilterEmployeeId
        {
            get => _filterEmployeeId;
            set { _filterEmployeeId = value; OnPropertyChanged(nameof(FilterEmployeeId)); }
        }

        public ICommand FilterCommand { get; }

        private readonly AttendanceService _attendanceService = new();

        public AttendanceAdminViewModel()
        {
            FilterCommand = new RelayCommand(_ => Filter());
            Filter();
        }

        private void Filter()
        {
            int? empId = null;
            if (int.TryParse(FilterEmployeeId, out int parsedId)) empId = parsedId;
            var results = _attendanceService.GetAttendances(FilterDate, empId);
            Attendances.Clear();
            foreach (var attendance in results)
                Attendances.Add(attendance);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string prop) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}
