using System.Windows.Controls;
using HillsCafeManagement.ViewModels;

namespace HillsCafeManagement.Views.Admin.Receipts
{
    public partial class Receipts : UserControl
    {
        public Receipts()
        {
            InitializeComponent();
            // Set VM in code-behind to avoid XAML CLR-type resolution issues during partial builds
            DataContext = new ReceiptsViewModel();
        }
    }
}
