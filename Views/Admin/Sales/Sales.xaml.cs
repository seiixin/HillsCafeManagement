using System.Windows.Controls;
using HillsCafeManagement.ViewModels;

namespace HillsCafeManagement.Views.Admin.Sales
{
    public partial class Sales : UserControl
    {
        public Sales()
        {
            InitializeComponent();
            DataContext = new SalesViewModel();
        }
    }
}
