#nullable enable
using System;
using System.Windows.Input;

namespace HillsCafeManagement.Helpers
{
    // -------- Non-generic --------
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;

        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

        public void Execute(object? parameter) => _execute(parameter);

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        /// <summary>Force WPF to requery CanExecute (use when state changes).</summary>
        public void RaiseCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();
    }

    // -------- Generic --------
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Predicate<T?>? _canExecute;

        public RelayCommand(Action<T?> execute, Predicate<T?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            var (ok, value) = TryCast(parameter);
            return ok && (_canExecute?.Invoke(value) ?? true);
        }

        public void Execute(object? parameter)
        {
            var (_, value) = TryCast(parameter);
            _execute(value);
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        /// <summary>Force WPF to requery CanExecute (use when state changes).</summary>
        public void RaiseCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();

        private static (bool ok, T? value) TryCast(object? parameter)
        {
            // Accept exact T
            if (parameter is T t) return (true, t);

            // Accept null for reference/nullable types
            if (parameter is null)
            {
                var isValueType = typeof(T).IsValueType && Nullable.GetUnderlyingType(typeof(T)) is null;
                return isValueType ? (false, default) : (true, default);
            }

            // Try Convert.ChangeType for common value-type scenarios (e.g., boxing from XAML)
            try
            {
                var targetType = typeof(T);
                var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

                if (parameter is IConvertible && typeof(IConvertible).IsAssignableFrom(underlying))
                {
                    var converted = (T?)Convert.ChangeType(parameter, underlying);
                    return (true, converted);
                }
            }
            catch { /* ignore */ }

            return (false, default);
        }
    }
}
