using System.Windows.Controls;
using HillsCafeManagement.ViewModels;

namespace HillsCafeManagement.Views.Cashier.Tables
{
    public partial class TablesView : UserControl
    {
        public TablesView()
        {
            InitializeComponent();
            DataContext = new TableViewModel(); // reuse your VM
        }
    }
}
