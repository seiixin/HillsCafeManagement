using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using HillsCafeManagement.Helpers;
using HillsCafeManagement.Models;
using HillsCafeManagement.Services;

namespace HillsCafeManagement.ViewModels
{
    public class AttendanceEmployeeViewModel : INotifyPropertyChanged
    {
        // ===== Commands =====
        public ICommand ClockInCommand { get; }
        public ICommand ClockOutCommand { get; }
        public ICommand RefreshListCommand { get; }
        public ICommand ApplyFilterCommand { get; }
        public ICommand ResetFilterCommand { get; }

        private readonly AttendanceService _attendanceService = new();

        // ===== UI State (existing) =====
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

        // ===== New: List + Filters =====
        public ObservableCollection<AttendanceModel> Attendances { get; } = new();

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        // Default range: last 30 days up to today
        private DateTime? _filterFromDate = DateTime.Today.AddDays(-30);
        public DateTime? FilterFromDate
        {
            get => _filterFromDate;
            set
            {
                if (_filterFromDate == value) return;
                _filterFromDate = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private DateTime? _filterToDate = DateTime.Today;
        public DateTime? FilterToDate
        {
            get => _filterToDate;
            set
            {
                if (_filterToDate == value) return;
                _filterToDate = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public AttendanceEmployeeViewModel()
        {
            // Keep existing behavior
            ClockInCommand = new RelayCommand(async _ => await ClockInAsync(), _ => CanExecuteAction());
            ClockOutCommand = new RelayCommand(async _ => await ClockOutAsync(), _ => CanExecuteAction());

            // New commands for the list
            RefreshListCommand = new RelayCommand(async _ => await RefreshListAsync(), _ => !IsLoading);
            ApplyFilterCommand = new RelayCommand(async _ => await RefreshListAsync(), _ => CanApplyFilter() && !IsLoading);
            ResetFilterCommand = new RelayCommand(_ => ResetFilter(), _ => !IsLoading);

            // Initial load
            _ = RefreshListAsync();
        }

        // ===== Clock In / Out =====
        private async Task ClockInAsync()
        {
            try
            {
                if (IsManualMode && TryGetManualDateTime(out var ts))
                    _attendanceService.ClockIn(Session.CurrentUserId, ts);
                else
                    _attendanceService.ClockIn(Session.CurrentUserId);

                LastMessage = $"✅ Clock In successful! ({DateTime.Now:hh:mm:ss tt})";

                // refresh records so the new entry shows up
                await RefreshListAsync();
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

                // refresh records so the updated row shows time_out
                await RefreshListAsync();
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

        // ===== New: Load/Filter list =====
        private bool CanApplyFilter()
        {
            if (FilterFromDate.HasValue && FilterToDate.HasValue)
            {
                // from must be <= to
                return FilterFromDate.Value.Date <= FilterToDate.Value.Date;
            }
            return true; // allow open-ended ranges
        }

        private void ResetFilter()
        {
            FilterFromDate = DateTime.Today.AddDays(-30);
            FilterToDate = DateTime.Today;
            _ = RefreshListAsync();
        }

        private async Task RefreshListAsync()
        {
            try
            {
                IsLoading = true;

                // Call your new service method; wrap in Task.Run to avoid UI freeze
                var items = await Task.Run(() =>
                    _attendanceService.GetAttendancesForEmployee(
                        Session.CurrentUserId,
                        FilterFromDate,
                        FilterToDate));

                // Replace collection contents (preserves binding)
                Attendances.Clear();
                foreach (var it in items)
                    Attendances.Add(it);

                LastMessage = $"📋 Loaded {Attendances.Count} record(s).";
            }
            catch (Exception ex)
            {
                LastMessage = $"❌ Failed to load records: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        // ===== INotifyPropertyChanged =====
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? prop = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}
