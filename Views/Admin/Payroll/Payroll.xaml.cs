using System.Windows.Controls;
using System.Windows;
using HillsCafeManagement.ViewModels;

namespace HillsCafeManagement.Views.Admin.Payroll
{
    public partial class Payroll : UserControl
    {
        private PayrollViewModel viewModel;

        public Payroll()
        {
            InitializeComponent();
            viewModel = new PayrollViewModel();
            DataContext = viewModel;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            string filter = textBox.Text.ToLower();

            viewModel.FilterPayroll(filter);
        }
    }
}
