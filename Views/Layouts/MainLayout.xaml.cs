using System.Windows;
using System.Windows.Controls;

namespace HillsCafeManagement.Views.Layouts
{
    public partial class MainLayout : UserControl
    {
        private bool isSidebarVisible = true;

        public MainLayout()
        {
            InitializeComponent();
        }

        private void ToggleSidebar_Click(object sender, RoutedEventArgs e)
        {
            isSidebarVisible = !isSidebarVisible;

            SidebarControl.Visibility = isSidebarVisible ? Visibility.Visible : Visibility.Collapsed;
            SidebarColumn.Width = isSidebarVisible ? new GridLength(250) : new GridLength(0);
        }
    }
}
