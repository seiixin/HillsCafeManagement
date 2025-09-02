using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using HillsCafeManagement.Models;
using HillsCafeManagement.ViewModels;

namespace HillsCafeManagement.Views.Admin.Attendance
{
    public partial class AttendanceAdminView : UserControl
    {
        private bool _savingRow;          // hard re-entrancy guard
        private bool _committingCell;     // soft guard to avoid cascading commits

        // Default ctor
        public AttendanceAdminView() : this(new AttendanceAdminViewModel()) { }

        // DI/test-friendly ctor
        public AttendanceAdminView(AttendanceAdminViewModel viewModel)
        {
            InitializeComponent();

            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            Loaded += (_, __) =>
            {
                if (DataContext is AttendanceAdminViewModel vm)
                {
                    vm.FilterCommand?.Execute(null);
                    vm.LeaveFilterCommand?.Execute(null);
                }
            };
        }

        private void LeaveGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_savingRow) return;
            if (DataContext is not AttendanceAdminViewModel vm) return;
            if (sender is not DataGrid grid) return;

            if (grid.SelectedItem is LeaveRequestModel row && !ReferenceEquals(vm.SelectedLeave, row))
            {
                vm.SelectedLeave = row;
            }
        }

        // Commit any pending cell edit when the current cell moves.
        private void LeaveGrid_CurrentCellChanged(object? sender, EventArgs e)
        {
            if (_committingCell) return;
            if (sender is not DataGrid grid) return;

            try
            {
                _committingCell = true;
                // This is safe and does not recurse if guarded.
                grid.CommitEdit(DataGridEditingUnit.Cell, true);
            }
            catch
            {
                // swallow; we just want best-effort commit of cell bindings
            }
            finally
            {
                _committingCell = false;
            }
        }

        private void LeaveGrid_RowEditEnding(object? sender, DataGridRowEditEndingEventArgs e)
        {
            if (_savingRow) return;
            if (sender is not DataGrid grid) return;
            if (DataContext is not AttendanceAdminViewModel vm) return;

            if (e.EditAction != DataGridEditAction.Commit)
                return;

            // IMPORTANT:
            // Defer updates until AFTER WPF fully commits the row, to avoid re-entrancy/crash.
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_savingRow) return;

                try
                {
                    _savingRow = true;

                    // Ensure all pending edits are committed.
                    grid.CommitEdit(DataGridEditingUnit.Cell, true);
                    grid.CommitEdit(DataGridEditingUnit.Row, true);

                    // Sync SelectedLeave to the just-edited row
                    if (e.Row?.Item is LeaveRequestModel row && !ReferenceEquals(vm.SelectedLeave, row))
                        vm.SelectedLeave = row;

                    // Execute the VM Update (DB write + refresh)
                    if (vm.LeaveUpdateCommand?.CanExecute(null) == true)
                        vm.LeaveUpdateCommand.Execute(null);
                }
                finally
                {
                    _savingRow = false;
                }
            }), DispatcherPriority.Background);
        }
    }
}
