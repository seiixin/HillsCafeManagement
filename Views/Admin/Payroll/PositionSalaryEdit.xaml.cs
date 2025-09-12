using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup; // XamlParseException
using HillsCafeManagement.Services;
using HillsCafeManagement.ViewModels;

namespace HillsCafeManagement.Views.Admin.Payroll
{
    public partial class PositionSalaryEdit : UserControl
    {
        public event Action? CloseRequested;

        /// <summary>Strongly-typed access to the view model.</summary>
        public PositionSalaryViewModel VM { get; }

        /// <summary>
        /// True if InitializeComponent failed (e.g., XAML parse/binding issue). Host may skip showing this control.
        /// </summary>
        public bool InitFailed { get; private set; }

        // Default ctor: safe, does not touch DB during construction.
        public PositionSalaryEdit() : this(service: null) { }

        /// <summary>
        /// DI-friendly ctor: supply a custom service (e.g., test DB, different connection string).
        /// </summary>
        public PositionSalaryEdit(PositionSalaryService? service)
        {
            try
            {
                InitializeComponent(); // If XAML has issues, we catch below instead of crashing the app.
            }
            catch (XamlParseException xpe)
            {
                var root = xpe.InnerException ?? xpe;
                MessageBox.Show(
                    $"UI load failed (XAML).\n\n{root.GetType().Name}: {root.Message}\n\n{root.StackTrace}",
                    "Positions & Salaries", MessageBoxButton.OK, MessageBoxImage.Error);
                InitFailed = true;
                return;
            }
            catch (Exception ex)
            {
                var root = ex.InnerException ?? ex;
                MessageBox.Show(
                    $"UI load failed.\n\n{root.GetType().Name}: {root.Message}\n\n{root.StackTrace}",
                    "Positions & Salaries", MessageBoxButton.OK, MessageBoxImage.Error);
                InitFailed = true;
                return;
            }

            VM = new PositionSalaryViewModel(service);
            VM.RequestClose += OnVmRequestClose;

            DataContext = VM;

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        /// <summary>
        /// Overload if you want to construct the VM elsewhere and pass it in.
        /// </summary>
        public PositionSalaryEdit(PositionSalaryViewModel viewModel)
        {
            try
            {
                InitializeComponent();
            }
            catch (XamlParseException xpe)
            {
                var root = xpe.InnerException ?? xpe;
                MessageBox.Show(
                    $"UI load failed (XAML).\n\n{root.GetType().Name}: {root.Message}\n\n{root.StackTrace}",
                    "Positions & Salaries", MessageBoxButton.OK, MessageBoxImage.Error);
                InitFailed = true;
                return;
            }
            catch (Exception ex)
            {
                var root = ex.InnerException ?? ex;
                MessageBox.Show(
                    $"UI load failed.\n\n{root.GetType().Name}: {root.Message}\n\n{root.StackTrace}",
                    "Positions & Salaries", MessageBoxButton.OK, MessageBoxImage.Error);
                InitFailed = true;
                return;
            }

            VM = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            VM.RequestClose += OnVmRequestClose;

            DataContext = VM;

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnVmRequestClose() => CloseRequested?.Invoke();

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            if (InitFailed) return; // Don't proceed if XAML failed to load.

            try
            {
                Keyboard.Focus(this);
                // Load AFTER the view is ready so DB/UI errors show as dialogs, not crashes.
                VM.Load();
            }
            catch (Exception ex)
            {
                var root = ex.InnerException ?? ex;
                MessageBox.Show(
                    $"Failed to open Positions & Salaries.\n\n{root.GetType().Name}: {root.Message}\n\n{root.StackTrace}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnUnloaded(object? sender, RoutedEventArgs e)
        {
            if (!InitFailed)
                VM.RequestClose -= OnVmRequestClose;

            Loaded -= OnLoaded;
            Unloaded -= OnUnloaded;
        }

        /// <summary>Allow ESC key to close the panel (host should handle CloseRequested).</summary>
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                CloseRequested?.Invoke();
                return;
            }
            base.OnPreviewKeyDown(e);
        }
    }
}
