using System.Windows;
using System.Windows.Controls;

namespace HillsCafeManagement.Views.Admin.Orders
{
    public partial class Orders : UserControl
    {
        public Orders()
        {
            InitializeComponent();
            // Just dummy data example (remove later)
            
        }

        private void AddOrder_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Add Order clicked!");
        }

        private void EditOrder_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Edit Order clicked!");
        }

        private void DeleteOrder_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Delete Order clicked!");
        }
    }
}
