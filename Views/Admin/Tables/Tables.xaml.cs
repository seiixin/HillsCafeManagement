using System.Windows.Controls;
using HillsCafeManagement.ViewModels;

namespace HillsCafeManagement.Views.Admin.Tables
{
    public partial class Tables : UserControl
    {
        public Tables()
        {
            InitializeComponent();
            DataContext = new TableViewModel(); // Admin uses TableViewModel
        }
    }
}
