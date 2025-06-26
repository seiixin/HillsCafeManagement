using HillsCafeManagement.Helpers;
using HillsCafeManagement.Services;
using System.Windows.Input;

namespace HillsCafeManagement.ViewModels
{
    public class AttendanceEmployeeViewModel
    {
        public ICommand ClockInCommand { get; }
        public ICommand ClockOutCommand { get; }

        private readonly AttendanceService _attendanceService = new();

        public AttendanceEmployeeViewModel()
        {
            ClockInCommand = new RelayCommand(_ => ClockIn());
            ClockOutCommand = new RelayCommand(_ => ClockOut());
        }

        private void ClockIn() => _attendanceService.ClockIn(Session.CurrentUserId);
        private void ClockOut() => _attendanceService.ClockOut(Session.CurrentUserId);
    }
}