using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
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
        // ===== Services =====
        private readonly AttendanceService _attendanceService = new();
        private readonly ILeaveRequestService _leaveService = new LeaveRequestService();
        private readonly IEmployeeService _employeeService = new EmployeeService();

        // ===== Resolved identity =====
        private int? _currentEmployeeId;
        public int? CurrentEmployeeId
        {
            get => _currentEmployeeId;
            private set { _currentEmployeeId = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); }
        }

        // ===== Commands (Attendance) =====
        public ICommand ClockInCommand { get; private set; } = null!;
        public ICommand ClockOutCommand { get; private set; } = null!;
        public ICommand RefreshListCommand { get; private set; } = null!;
        public ICommand ApplyFilterCommand { get; private set; } = null!;
        public ICommand ResetFilterCommand { get; private set; } = null!;

        // ===== Commands (Leave) =====
        public ICommand LoadLeaveListCommand { get; private set; } = null!;
        public ICommand ApplyLeaveFilterCommand { get; private set; } = null!;
        public ICommand ResetLeaveFilterCommand { get; private set; } = null!;
        public ICommand NewLeaveFormCommand { get; private set; } = null!;
        public ICommand SubmitLeaveRequestCommand { get; private set; } = null!;
        public ICommand UpdateLeaveRequestCommand { get; private set; } = null!;
        public ICommand CancelLeaveRequestCommand { get; private set; } = null!;
        public ICommand EditSelectedLeaveCommand { get; private set; } = null!;

        // ===== UI State (Shared) =====
        private string _lastMessage = string.Empty;
        public string LastMessage
        {
            get => _lastMessage;
            set { _lastMessage = value; OnPropertyChanged(); }
        }

        private bool _isBusyAttendance;
        public bool IsBusyAttendance
        {
            get => _isBusyAttendance;
            set { _isBusyAttendance = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); }
        }

        private bool _isBusyLeave;
        public bool IsBusyLeave
        {
            get => _isBusyLeave;
            set { _isBusyLeave = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); }
        }

        // =========================================================================================
        // ATTENDANCE SECTION
        // =========================================================================================

        private bool _isManualMode;
        public bool IsManualMode
        {
            get => _isManualMode;
            set { if (_isManualMode != value) { _isManualMode = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); } }
        }

        private DateTime? _manualDate = DateTime.Today;
        public DateTime? ManualDate
        {
            get => _manualDate;
            set { if (_manualDate != value) { _manualDate = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); } }
        }

        // Accepts "HH:mm" or "HH:mm:ss"
        private string _manualTimeText = "09:00";
        public string ManualTimeText
        {
            get => _manualTimeText;
            set { if (_manualTimeText != value) { _manualTimeText = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); } }
        }

        public ObservableCollection<AttendanceModel> Attendances { get; } = new();

        private DateTime? _filterFromDate = DateTime.Today.AddDays(-30);
        public DateTime? FilterFromDate
        {
            get => _filterFromDate;
            set { if (_filterFromDate != value) { _filterFromDate = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); } }
        }

        private DateTime? _filterToDate = DateTime.Today;
        public DateTime? FilterToDate
        {
            get => _filterToDate;
            set { if (_filterToDate != value) { _filterToDate = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); } }
        }

        // =========================================================================================
        // LEAVE REQUESTS SECTION
        // =========================================================================================

        public ObservableCollection<LeaveRequestModel> LeaveRequests { get; } = new();

        private DateTime? _leaveFilterFrom = DateTime.Today.AddDays(-60);
        public DateTime? LeaveFilterFrom
        {
            get => _leaveFilterFrom;
            set { if (_leaveFilterFrom != value) { _leaveFilterFrom = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); } }
        }

        private DateTime? _leaveFilterTo = DateTime.Today.AddDays(60);
        public DateTime? LeaveFilterTo
        {
            get => _leaveFilterTo;
            set { if (_leaveFilterTo != value) { _leaveFilterTo = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); } }
        }

        // null = All
        private LeaveStatus? _leaveFilterStatus = null;
        public LeaveStatus? LeaveFilterStatus
        {
            get => _leaveFilterStatus;
            set { if (_leaveFilterStatus != value) { _leaveFilterStatus = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); } }
        }

        // Selected row
        private LeaveRequestModel? _selectedLeave;
        public LeaveRequestModel? SelectedLeave
        {
            get => _selectedLeave;
            set { _selectedLeave = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); }
        }

        // Leave form (create/update)
        private int _leaveFormId; // 0 => new
        public int LeaveFormId
        {
            get => _leaveFormId;
            set { _leaveFormId = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); }
        }

        private LeaveType _leaveType = LeaveType.Sick;
        public LeaveType LeaveType
        {
            get => _leaveType;
            set { _leaveType = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); }
        }

        private string? _leaveReason;
        public string? LeaveReason
        {
            get => _leaveReason;
            set { _leaveReason = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); }
        }

        private DateTime _leaveDateFrom = DateTime.Today;
        public DateTime LeaveDateFrom
        {
            get => _leaveDateFrom;
            set { _leaveDateFrom = value.Date; OnPropertyChanged(); OnPropertyChanged(nameof(LeaveDurationDays)); CommandManager.InvalidateRequerySuggested(); }
        }

        private DateTime _leaveDateTo = DateTime.Today;
        public DateTime LeaveDateTo
        {
            get => _leaveDateTo;
            set { _leaveDateTo = value.Date; OnPropertyChanged(); OnPropertyChanged(nameof(LeaveDurationDays)); CommandManager.InvalidateRequerySuggested(); }
        }

        private bool _leaveHalfDay;
        public bool LeaveHalfDay
        {
            get => _leaveHalfDay;
            set { _leaveHalfDay = value; OnPropertyChanged(); OnPropertyChanged(nameof(LeaveDurationDays)); CommandManager.InvalidateRequerySuggested(); }
        }

        public string LeaveDurationDays
        {
            get
            {
                if (LeaveHalfDay) return "0.5 day";
                var days = (LeaveDateTo.Date - LeaveDateFrom.Date).TotalDays + 1;
                if (days < 0) return "—";
                return $"{days:0} day(s)";
            }
        }

        // =========================================================================================
        // ctors
        // =========================================================================================

        // Default: resolve employee via users.employee_id
        public AttendanceEmployeeViewModel()
        {
            ResolveEmployeeIdFromDb();
            SetupCommands();
            KickOffInitialLoads();
        }

        // NEW: accept employeeId directly (used by Sidebar)
        public AttendanceEmployeeViewModel(int employeeId)
        {
            CurrentEmployeeId = employeeId > 0 ? employeeId : null;
            if (CurrentEmployeeId is null)
                ResolveEmployeeIdFromDb();

            SetupCommands();
            KickOffInitialLoads();
        }

        private void ResolveEmployeeIdFromDb()
        {
            CurrentEmployeeId = _employeeService.GetEmployeeIdByUserId(Session.CurrentUserId);
            if (CurrentEmployeeId is null)
                LastMessage = "⚠️ No employee profile linked to your user. Please contact admin.";
        }

        private void SetupCommands()
        {
            // Attendance
            ClockInCommand = new RelayCommand(async _ => await ClockInAsync(), _ => CanExecuteAttendanceAction() && !IsBusyAttendance);
            ClockOutCommand = new RelayCommand(async _ => await ClockOutAsync(), _ => CanExecuteAttendanceAction() && !IsBusyAttendance);
            RefreshListCommand = new RelayCommand(async _ => await RefreshAttendanceAsync(), _ => !IsBusyAttendance);
            ApplyFilterCommand = new RelayCommand(async _ => await RefreshAttendanceAsync(), _ => !IsBusyAttendance && CanApplyAttendanceFilter());
            ResetFilterCommand = new RelayCommand(_ => ResetAttendanceFilter(), _ => !IsBusyAttendance);

            // Leave
            LoadLeaveListCommand = new RelayCommand(async _ => await RefreshLeaveListAsync(), _ => !IsBusyLeave);
            ApplyLeaveFilterCommand = new RelayCommand(async _ => await RefreshLeaveListAsync(), _ => !IsBusyLeave && CanApplyLeaveFilter());
            ResetLeaveFilterCommand = new RelayCommand(_ => ResetLeaveFilter(), _ => !IsBusyLeave);

            NewLeaveFormCommand = new RelayCommand(_ => ResetLeaveForm(), _ => !IsBusyLeave);
            SubmitLeaveRequestCommand = new RelayCommand(async _ => await SubmitLeaveAsync(), _ => !IsBusyLeave && CanSubmitLeave());
            UpdateLeaveRequestCommand = new RelayCommand(async _ => await UpdateLeaveAsync(), _ => !IsBusyLeave && CanUpdateLeave());
            CancelLeaveRequestCommand = new RelayCommand(async _ => await CancelLeaveAsync(), _ => !IsBusyLeave && CanCancelLeave());
            EditSelectedLeaveCommand = new RelayCommand(_ => LoadSelectedIntoForm(), _ => SelectedLeave != null && !IsBusyLeave);
        }

        private void KickOffInitialLoads()
        {
            _ = RefreshAttendanceAsync();
            _ = RefreshLeaveListAsync();
        }

        // =========================================================================================
        // ATTENDANCE LOGIC
        // =========================================================================================
        private bool CanExecuteAttendanceAction()
        {
            if (!IsManualMode) return true;
            return TryGetManualDateTime(out _);
        }

        private bool TryGetManualDateTime(out DateTime result)
        {
            result = DateTime.Now;
            if (!IsManualMode) return true;
            if (ManualDate is null) return false;

            var formats = new[] { "HH:mm", "HH:mm:ss" };
            if (!DateTime.TryParseExact(ManualTimeText ?? string.Empty, formats, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var t))
                return false;

            var d = ManualDate.Value.Date;
            result = new DateTime(d.Year, d.Month, d.Day, t.Hour, t.Minute, t.Second);
            return true;
        }

        private async Task ClockInAsync()
        {
            try
            {
                IsBusyAttendance = true;

                // NOTE: Your AttendanceService currently uses userId; keep it as-is for now.
                if (IsManualMode && TryGetManualDateTime(out var ts))
                    _attendanceService.ClockIn(Session.CurrentUserId, ts);
                else
                    _attendanceService.ClockIn(Session.CurrentUserId);

                LastMessage = $"✅ Clock In successful! ({DateTime.Now:hh:mm:ss tt})";
                await RefreshAttendanceAsync();
            }
            catch (Exception ex)
            {
                LastMessage = $"❌ Clock In failed: {ex.Message}";
            }
            finally { IsBusyAttendance = false; _ = ClearMessageAfterDelay(); }
        }

        private async Task ClockOutAsync()
        {
            try
            {
                IsBusyAttendance = true;

                if (IsManualMode && TryGetManualDateTime(out var ts))
                    _attendanceService.ClockOut(Session.CurrentUserId, ts);
                else
                    _attendanceService.ClockOut(Session.CurrentUserId);

                LastMessage = $"✅ Clock Out successful! ({DateTime.Now:hh:mm:ss tt})";
                await RefreshAttendanceAsync();
            }
            catch (Exception ex)
            {
                LastMessage = $"❌ Clock Out failed: {ex.Message}";
            }
            finally { IsBusyAttendance = false; _ = ClearMessageAfterDelay(); }
        }

        private bool CanApplyAttendanceFilter()
        {
            if (FilterFromDate.HasValue && FilterToDate.HasValue)
                return FilterFromDate.Value.Date <= FilterToDate.Value.Date;
            return true;
        }

        private void ResetAttendanceFilter()
        {
            FilterFromDate = DateTime.Today.AddDays(-30);
            FilterToDate = DateTime.Today;
            _ = RefreshAttendanceAsync();
        }

        private async Task RefreshAttendanceAsync()
        {
            try
            {
                IsBusyAttendance = true;

                // If your attendance table uses employee_id, switch to CurrentEmployeeId!.Value here.
                var items = await Task.Run(() =>
                    _attendanceService.GetAttendancesForEmployee(Session.CurrentUserId, FilterFromDate, FilterToDate));

                Attendances.Clear();
                foreach (var it in items) Attendances.Add(it);

                LastMessage = $"📋 Loaded {Attendances.Count} attendance record(s).";
            }
            catch (Exception ex)
            {
                LastMessage = $"❌ Failed to load attendance: {ex.Message}";
            }
            finally { IsBusyAttendance = false; }
        }

        // =========================================================================================
        // LEAVE LOGIC
        // =========================================================================================
        private bool CanApplyLeaveFilter()
        {
            if (LeaveFilterFrom.HasValue && LeaveFilterTo.HasValue)
                return LeaveFilterFrom.Value.Date <= LeaveFilterTo.Value.Date;
            return true;
        }

        private void ResetLeaveFilter()
        {
            LeaveFilterFrom = DateTime.Today.AddDays(-60);
            LeaveFilterTo = DateTime.Today.AddDays(60);
            LeaveFilterStatus = null; // All
            _ = RefreshLeaveListAsync();
        }

        private async Task RefreshLeaveListAsync()
        {
            try
            {
                IsBusyLeave = true;

                if (CurrentEmployeeId is null) { LeaveRequests.Clear(); return; }

                var list = await Task.Run(() =>
                    _leaveService.GetForEmployee(CurrentEmployeeId.Value, LeaveFilterFrom, LeaveFilterTo, LeaveFilterStatus));

                LeaveRequests.Clear();
                foreach (var lr in list) LeaveRequests.Add(lr);

                LastMessage = $"🗂 Loaded {LeaveRequests.Count} leave request(s).";
            }
            catch (Exception ex)
            {
                LastMessage = $"❌ Failed to load leaves: {ex.Message}";
            }
            finally { IsBusyLeave = false; }
        }

        private void ResetLeaveForm()
        {
            LeaveFormId = 0;
            LeaveType = LeaveType.Sick;
            LeaveReason = string.Empty;
            LeaveDateFrom = DateTime.Today;
            LeaveDateTo = DateTime.Today;
            LeaveHalfDay = false;
            OnPropertyChanged(nameof(LeaveDurationDays));
        }

        private void LoadSelectedIntoForm()
        {
            if (SelectedLeave == null) return;

            LeaveFormId = SelectedLeave.Id;
            LeaveType = SelectedLeave.LeaveType;
            LeaveReason = SelectedLeave.Reason;
            LeaveDateFrom = SelectedLeave.DateFrom.Date;
            LeaveDateTo = SelectedLeave.DateTo.Date;
            LeaveHalfDay = SelectedLeave.HalfDay;
            OnPropertyChanged(nameof(LeaveDurationDays));
        }

        private bool CanSubmitLeave()
        {
            if (LeaveFormId != 0) return false; // new only
            if (LeaveDateFrom.Date > LeaveDateTo.Date) return false;
            if (LeaveHalfDay && LeaveDateFrom.Date != LeaveDateTo.Date) return false;
            return CurrentEmployeeId.HasValue; // must have employee link
        }

        private async Task SubmitLeaveAsync()
        {
            try
            {
                IsBusyLeave = true;

                if (CurrentEmployeeId is null) { LastMessage = "❌ No linked employee profile."; return; }

                // basic overlap guard against pending/approved
                var overlaps = await Task.Run(() =>
                    _leaveService.GetForEmployee(CurrentEmployeeId.Value, LeaveDateFrom, LeaveDateTo, null)
                                 .Any(x => x.Status == LeaveStatus.Approved || x.Status == LeaveStatus.Pending));
                if (overlaps) { LastMessage = "⚠️ Overlaps with existing pending/approved leave."; return; }

                var model = new LeaveRequestModel
                {
                    EmployeeId = CurrentEmployeeId.Value, // <-- EMPLOYEE id (not user id)
                    LeaveType = LeaveType,
                    Reason = string.IsNullOrWhiteSpace(LeaveReason) ? null : LeaveReason.Trim(),
                    DateFrom = LeaveDateFrom.Date,
                    DateTo = LeaveDateTo.Date,
                    HalfDay = LeaveHalfDay,
                    Status = LeaveStatus.Pending
                };

                var id = await Task.Run(() => _leaveService.Create(model));
                LeaveFormId = id;

                LastMessage = "✅ Leave request submitted (Pending).";
                await RefreshLeaveListAsync();
            }
            catch (Exception ex)
            {
                LastMessage = $"❌ Submit failed: {ex.Message}";
            }
            finally { IsBusyLeave = false; _ = ClearMessageAfterDelay(); }
        }

        private bool CanUpdateLeave()
        {
            if (LeaveFormId <= 0) return false;
            var row = LeaveRequests.FirstOrDefault(x => x.Id == LeaveFormId);
            if (row == null || row.Status != LeaveStatus.Pending) return false;
            if (LeaveDateFrom.Date > LeaveDateTo.Date) return false;
            if (LeaveHalfDay && LeaveDateFrom.Date != LeaveDateTo.Date) return false;
            return true;
        }

        private async Task UpdateLeaveAsync()
        {
            try
            {
                IsBusyLeave = true;

                var row = LeaveRequests.FirstOrDefault(x => x.Id == LeaveFormId);
                if (row == null) { LastMessage = "⚠️ Leave request not found."; return; }
                if (row.Status != LeaveStatus.Pending) { LastMessage = "⚠️ Only pending requests can be edited."; return; }

                row.LeaveType = LeaveType;
                row.Reason = string.IsNullOrWhiteSpace(LeaveReason) ? null : LeaveReason.Trim();
                row.DateFrom = LeaveDateFrom.Date;
                row.DateTo = LeaveDateTo.Date;
                row.HalfDay = LeaveHalfDay;

                var ok = await Task.Run(() => _leaveService.Update(row));
                LastMessage = ok ? "✅ Leave request updated." : "❌ Update failed.";
                await RefreshLeaveListAsync();
            }
            catch (Exception ex)
            {
                LastMessage = $"❌ Update failed: {ex.Message}";
            }
            finally { IsBusyLeave = false; _ = ClearMessageAfterDelay(); }
        }

        private bool CanCancelLeave()
        {
            var row = SelectedLeave ?? LeaveRequests.FirstOrDefault(x => x.Id == LeaveFormId);
            return row != null && row.Status != LeaveStatus.Cancelled;
        }

        private async Task CancelLeaveAsync()
        {
            try
            {
                IsBusyLeave = true;

                var row = SelectedLeave ?? LeaveRequests.FirstOrDefault(x => x.Id == LeaveFormId);
                if (row == null) { LastMessage = "⚠️ Select a leave request to cancel."; return; }

                var ok = await Task.Run(() => _leaveService.Cancel(row.Id));
                LastMessage = ok ? "✅ Leave request cancelled." : "❌ Cancel failed.";
                await RefreshLeaveListAsync();

                if (row.Id == LeaveFormId)
                {
                    var refreshed = LeaveRequests.FirstOrDefault(x => x.Id == LeaveFormId);
                    if (refreshed != null)
                    {
                        LeaveType = refreshed.LeaveType;
                        LeaveReason = refreshed.Reason;
                        LeaveDateFrom = refreshed.DateFrom.Date;
                        LeaveDateTo = refreshed.DateTo.Date;
                        LeaveHalfDay = refreshed.HalfDay;
                        OnPropertyChanged(nameof(LeaveDurationDays));
                    }
                }
            }
            catch (Exception ex)
            {
                LastMessage = $"❌ Cancel failed: {ex.Message}";
            }
            finally { IsBusyLeave = false; _ = ClearMessageAfterDelay(); }
        }

        // ===== Helpers =====
        private async Task ClearMessageAfterDelay(int ms = 4000)
        {
            try { await Task.Delay(ms); LastMessage = string.Empty; } catch { }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? prop = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}
