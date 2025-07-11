using System.Windows.Controls;

namespace HillsCafeManagement.Views.Admin.Tables
{
    /// <summary>
    /// Interaction logic for Tables.xaml
    /// </summary>
    public partial class Tables : UserControl
    {
        public Tables()
        {
            InitializeComponent();
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }

    public class TableItem
    {
        public string TableNumber { get; set; }
        public string Capacity { get; set; }
        public string Status { get; set; } = string.Empty;
    }

}
