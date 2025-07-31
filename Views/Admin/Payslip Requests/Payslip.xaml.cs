using HillsCafeManagement.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace HillsCafeManagement.Views.Admin.Payslip_Requests
{
    public partial class Payslip : UserControl
    {
        private AdminPayslipViewModel viewModel;

        public Payslip()
        {
            InitializeComponent();

            viewModel = new AdminPayslipViewModel();
            this.DataContext = viewModel;

            Loaded += Payslip_Loaded;
        }

        private void Payslip_Loaded(object sender, RoutedEventArgs e)
        {
            viewModel.LoadPayslipRequestsFromDatabase();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (viewModel != null && sender is TextBox tb)
            {
                viewModel.FilterPayslipRequests(tb.Text);
            }
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
