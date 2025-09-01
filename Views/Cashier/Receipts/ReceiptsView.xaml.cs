using System.Windows;
using System.Windows.Controls;
using HillsCafeManagement.ViewModels;

namespace HillsCafeManagement.Views.Cashier.Receipts
{
    public partial class ReceiptsView : UserControl
    {
        public ReceiptsView()
        {
            InitializeComponent();

            // Use same VM as Admin so behavior stays in sync
            DataContext = new ReceiptsViewModel();

            // Optional: auto-refresh on load if supported by the VM
            Loaded += (s, e) =>
            {
                if (DataContext is ReceiptsViewModel vm && vm.RefreshCommand?.CanExecute(null) == true)
                    vm.RefreshCommand.Execute(null);
            };
        }
    }
}