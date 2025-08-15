using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using HillsCafeManagement.Helpers;
using HillsCafeManagement.Services;

namespace HillsCafeManagement.ViewModels
{
    public class AttendanceEmployeeViewModel : INotifyPropertyChanged
    {
        public ICommand ClockInCommand { get; }
        public ICommand ClockOutCommand { get; }

        private readonly AttendanceService _attendanceService = new();

        // ===== UI State =====
        private bool _isManualMode;
        public bool IsManualMode
        {
            get => _isManualMode;
            set
            {
                if (_isManualMode == value) return;
                _isManualMode = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private DateTime? _manualDate = DateTime.Today;
        public DateTime? ManualDate
        {
            get => _manualDate;
            set
            {
                if (_manualDate == value) return;
                _manualDate = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        // Accepts "HH:mm" or "HH:mm:ss"
        private string _manualTimeText = "09:00";
        public string ManualTimeText
        {
            get => _manualTimeText;
            set
            {
                if (_manualTimeText == value) return;
                _manualTimeText = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        // Feedback message shown in the UI
        private string _lastMessage;
        public string LastMessage
        {
            get => _lastMessage;
            set { _lastMessage = value; OnPropertyChanged(); }
        }

        public AttendanceEmployeeViewModel()
        {
            // Use async lambdas so we can await and show messages without blocking UI
            ClockInCommand = new RelayCommand(async _ => await ClockInAsync(), _ => CanExecuteAction());
            ClockOutCommand = new RelayCommand(async _ => await ClockOutAsync(), _ => CanExecuteAction());
        }

        private async Task ClockInAsync()
        {
            try
            {
                if (IsManualMode && TryGetManualDateTime(out var ts))
                    _attendanceService.ClockIn(Session.CurrentUserId, ts);
                else
                    _attendanceService.ClockIn(Session.CurrentUserId);

                LastMessage = $"✅ Clock In successful! ({DateTime.Now:hh:mm:ss tt})";
            }
            catch (Exception ex)
            {
                LastMessage = $"❌ Clock In failed: {ex.Message}";
            }

            await ClearMessageAfterDelay();
        }

        private async Task ClockOutAsync()
        {
            try
            {
                if (IsManualMode && TryGetManualDateTime(out var ts))
                    _attendanceService.ClockOut(Session.CurrentUserId, ts);
                else
                    _attendanceService.ClockOut(Session.CurrentUserId);

                LastMessage = $"✅ Clock Out successful! ({DateTime.Now:hh:mm:ss tt})";
            }
            catch (Exception ex)
            {
                LastMessage = $"❌ Clock Out failed: {ex.Message}";
            }

            await ClearMessageAfterDelay();
        }

        // Buttons enabled when:
        // - Manual mode OFF  => always true
        // - Manual mode ON   => manual date+time parse successfully
        private bool CanExecuteAction()
        {
            if (!IsManualMode) return true;
            return TryGetManualDateTime(out _);
        }

        private bool TryGetManualDateTime(out DateTime result)
        {
            result = DateTime.Now;
            if (!IsManualMode) return true; // not used in this path, but keep consistent
            if (ManualDate is null) return false;

            var formats = new[] { "HH:mm", "HH:mm:ss" };
            if (!DateTime.TryParseExact(ManualTimeText ?? string.Empty, formats,
                                        CultureInfo.InvariantCulture,
                                        DateTimeStyles.None, out var t))
            {
                return false;
            }

            var d = ManualDate.Value.Date;
            result = new DateTime(d.Year, d.Month, d.Day, t.Hour, t.Minute, t.Second);
            return true;
        }

        private async Task ClearMessageAfterDelay(int milliseconds = 4000)
        {
            try
            {
                await Task.Delay(milliseconds);
                LastMessage = string.Empty;
            }
            catch
            {
                // ignore
            }
        }

        // ===== INotifyPropertyChanged =====
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? prop = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}
