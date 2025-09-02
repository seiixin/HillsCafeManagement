using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Win32;

using HillsCafeManagement.Models;
using HillsCafeManagement.Services;
using HillsCafeManagement.Helpers; // <- assumes your existing RelayCommand lives here

namespace HillsCafeManagement.ViewModels
{
    public class EmployeeProfileViewModel : INotifyPropertyChanged
    {
        private readonly EmployeeService _service = new EmployeeService();

        // -----------------------------
        // Bindable state
        // -----------------------------
        private int _employeeId;
        public int EmployeeId
        {
            get => _employeeId;
            set
            {
                if (Set(ref _employeeId, value))
                {
                    // auto-load when bound id changes
                    if (_employeeId > 0) LoadEmployee(_employeeId);
                }
            }
        }

        private EmployeeModel? _employee;
        public EmployeeModel? Employee
        {
            get => _employee;
            set => Set(ref _employee, value);
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set { if (Set(ref _isBusy, value)) RaiseCanExecutes(); }
        }

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set => Set(ref _statusMessage, value);
        }

        // -----------------------------
        // Commands
        // -----------------------------
        public ICommand LoadCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand ChangePhotoCommand { get; }
        public ICommand RefreshCommand { get; }

        public EmployeeProfileViewModel()
        {
            LoadCommand = new RelayCommand(_ => LoadEmployee(EmployeeId), _ => !IsBusy && EmployeeId > 0);
            SaveCommand = new RelayCommand(_ => SaveEmployee(), _ => !IsBusy && Employee != null && Employee.Id > 0);
            ChangePhotoCommand = new RelayCommand(_ => ChangePhoto(), _ => !IsBusy && Employee != null && Employee.Id > 0);
            RefreshCommand = new RelayCommand(_ => LoadEmployee(EmployeeId), _ => !IsBusy && EmployeeId > 0);
        }

        // -----------------------------
        // Ops
        // -----------------------------
        private void LoadEmployee(int id)
        {
            if (id <= 0) { StatusMessage = "Invalid employee id."; return; }
            try
            {
                IsBusy = true;

                // If you don’t use GetEmployeeById(), this still compiles; it just falls back to list + find.
                var one = _service.GetEmployeeById(id);
                if (one == null)
                {
                    var list = _service.GetAllEmployees();
                    one = list.Find(e => e.Id == id);
                }

                if (one == null)
                {
                    StatusMessage = $"Employee #{id} not found.";
                    Employee = null;
                }
                else
                {
                    Employee = one;
                    StatusMessage = $"Loaded employee #{id}.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Load failed: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void SaveEmployee()
        {
            if (Employee == null || Employee.Id <= 0)
            {
                StatusMessage = "Nothing to save.";
                return;
            }

            try
            {
                IsBusy = true;
                var ok = _service.UpdateEmployee(Employee);
                StatusMessage = ok ? "Profile updated." : "No changes were saved.";
                if (ok)
                {
                    // reload to reflect DB state
                    LoadEmployee(Employee.Id);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Save failed: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ChangePhoto()
        {
            if (Employee == null || Employee.Id <= 0)
            {
                StatusMessage = "Load an employee first.";
                return;
            }

            try
            {
                var ofd = new OpenFileDialog
                {
                    Title = "Choose profile photo",
                    Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif",
                    CheckFileExists = true,
                    Multiselect = false
                };

                if (ofd.ShowDialog() != true) return;

                IsBusy = true;

                // Save path (or copy to your assets folder if you prefer)
                var path = ofd.FileName;

                // Update DB image first to keep data safe
                var ok = _service.UpdateEmployeeImage(Employee.Id, path);
                if (ok)
                {
                    // reflect in UI
                    Employee.ImageUrl = path;
                    OnPropertyChanged(nameof(Employee));
                    StatusMessage = "Photo updated.";
                }
                else
                {
                    StatusMessage = "Failed to update photo.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Photo update failed: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        // -----------------------------
        // Helpers
        // -----------------------------
        private void RaiseCanExecutes()
        {
            // If your RelayCommand exposes RaiseCanExecuteChanged(), call it here.
            (LoadCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (ChangePhotoCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (RefreshCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        // -----------------------------
        // INotifyPropertyChanged
        // -----------------------------
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        protected bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(name);
            return true;
        }
    }
}
