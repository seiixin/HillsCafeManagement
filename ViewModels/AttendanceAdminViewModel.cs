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

        private AttendanceModel _selectedAttendance;
        public AttendanceModel SelectedAttendance
        {
            get => _selectedAttendance;
            set { _selectedAttendance = value; OnPropertyChanged(nameof(SelectedAttendance)); }
        }

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
        public ICommand AddCommand { get; }
        public ICommand UpdateCommand { get; }
        public ICommand DeleteCommand { get; }

        private readonly AttendanceService _attendanceService = new();

        public AttendanceAdminViewModel()
        {
            FilterCommand = new RelayCommand(_ => Filter());
            AddCommand = new RelayCommand(_ => AddAttendance(), _ => SelectedAttendance != null);
            UpdateCommand = new RelayCommand(_ => UpdateAttendance(), _ => SelectedAttendance != null);
            DeleteCommand = new RelayCommand(_ => DeleteAttendance(), _ => SelectedAttendance != null);
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

        private void AddAttendance()
        {
            _attendanceService.AddAttendance(SelectedAttendance);
            Filter();
        }

        private void UpdateAttendance()
        {
            _attendanceService.UpdateAttendance(SelectedAttendance);
            Filter();
        }

        private void DeleteAttendance()
        {
            _attendanceService.DeleteAttendance(SelectedAttendance.Id);
            Filter();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string prop) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}
