using System.Windows;
using System.Windows.Controls;
using HillsCafeManagement.ViewModels;

namespace HillsCafeManagement.Views.Employee.Payslip
{
    public partial class PayslipView : UserControl
    {
        public PayslipView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is EmployeePayslipViewModel vm)
            {
                vm.LoadPayslipsFromDatabase();
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (DataContext is EmployeePayslipViewModel vm)
            {
                vm.FilterPayslips(((TextBox)sender).Text);
            }
        }
    }
}
