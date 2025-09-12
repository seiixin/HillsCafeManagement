using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using HillsCafeManagement.Helpers;   // RelayCommand
using HillsCafeManagement.Services;
using MySql.Data.MySqlClient;

namespace HillsCafeManagement.ViewModels
{
    public sealed class PositionSalaryViewModel : INotifyPropertyChanged
    {
        private readonly PositionSalaryService _service;

        public ObservableCollection<PositionSalaryService.PositionSalary> Rates { get; } = new();

        private PositionSalaryService.PositionSalary? _selected;
        public PositionSalaryService.PositionSalary? Selected
        {
            get => _selected;
            set { _selected = value; OnPropertyChanged(); }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (_isBusy == value) return;
                _isBusy = value;
                OnPropertyChanged();
                RefreshCanExec();
            }
        }

        private string? _status;
        public string? Status
        {
            get => _status;
            private set { _status = value; OnPropertyChanged(); }
        }

        // Commands (typed RelayCommand para may RaiseCanExecuteChanged)
        public RelayCommand ReloadCommand { get; }
        public RelayCommand AddCommand { get; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand CloseCommand { get; }
        public RelayCommand<PositionSalaryService.PositionSalary> DeactivateCommand { get; }
        public RelayCommand<PositionSalaryService.PositionSalary> RemoveCommand { get; }

        public event Action? RequestClose;

        public PositionSalaryViewModel(PositionSalaryService? service = null)
        {
            _service = service ?? new PositionSalaryService();

            ReloadCommand = new RelayCommand(_ => Load());
            AddCommand = new RelayCommand(_ => AddNew());
            SaveCommand = new RelayCommand(_ => Save(), _ => CanSave());
            CloseCommand = new RelayCommand(_ => RequestClose?.Invoke());
            DeactivateCommand = new RelayCommand<PositionSalaryService.PositionSalary>(Deactivate, x => x is not null);
            RemoveCommand = new RelayCommand<PositionSalaryService.PositionSalary>(Remove, x => x is not null);

            // Re-evaluate Save enablement whenever collection changes
            Rates.CollectionChanged += (_, __) => RefreshCanExec();
            // Huwag mag-Load() dito; tawagin sa View.Loaded para iwas crash.
        }

        private bool CanSave() => Rates.Count > 0 && !IsBusy;

        private void RefreshCanExec()
        {
            try { SaveCommand.RaiseCanExecuteChanged(); }
            catch { CommandManager.InvalidateRequerySuggested(); } // fallback
        }

        // ===== Core ops =====
        public void Load()
        {
            IsBusy = true;
            Status = "Loading…";
            try
            {
                Rates.Clear();
                foreach (var r in _service.Load())
                    Rates.Add(r);

                Status = $"Loaded {Rates.Count} item(s).";
            }
            catch (MySqlException mex)
            {
                Status = "DB error";
                MessageBox.Show($"Database error (#{mex.Number}): {mex.Message}",
                    "Positions & Salaries", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                Status = "Failed to load.";
                MessageBox.Show($"Failed to load positions:\n{ex.Message}",
                    "Positions & Salaries", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
                RefreshCanExec();
            }
        }

        private void AddNew()
        {
            var item = new PositionSalaryService.PositionSalary
            {
                Position = string.Empty,
                DailyRate = 0m,
                IsActive = true,
                UpdatedAt = DateTime.Now
            };
            Rates.Add(item);
            Selected = item;
            Status = "New row added.";
            RefreshCanExec();
        }

        private void Save()
        {
            try
            {
                IsBusy = true;

                var invalid = Rates.Where(r => string.IsNullOrWhiteSpace(r.Position) || r.DailyRate < 0m).ToList();
                if (invalid.Count > 0)
                {
                    MessageBox.Show("Please ensure each row has a Position and a non-negative Daily Rate.",
                        "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                foreach (var r in Rates) r.UpdatedAt = DateTime.Now;

                _service.Save(Rates);

                Status = $"Saved {Rates.Count} item(s) at {DateTime.Now:HH:mm}.";
                MessageBox.Show("Saved successfully.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (MySqlException mex)
            {
                Status = "DB error";
                MessageBox.Show($"Save failed (DB #{mex.Number}): {mex.Message}",
                    "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                Status = "Save failed.";
                MessageBox.Show($"Failed to save:\n{ex.Message}",
                    "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
                RefreshCanExec();
            }
        }

        private void Deactivate(PositionSalaryService.PositionSalary? item)
        {
            if (item is null) return;
            item.IsActive = false;
            item.UpdatedAt = DateTime.Now;
            Status = $"Deactivated “{item.Position}”. Save to apply.";
            RefreshCanExec();
        }

        private void Remove(PositionSalaryService.PositionSalary? item)
        {
            if (item is null) return;

            var confirm = MessageBox.Show(
                $"Remove “{item.Position}” from this list?\n(This does not delete from DB until you Save.)",
                "Confirm Remove", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            Rates.Remove(item);
            if (ReferenceEquals(Selected, item)) Selected = null;
            Status = "Row removed. Save to apply.";
            RefreshCanExec();
        }

        // ===== INotifyPropertyChanged =====
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
