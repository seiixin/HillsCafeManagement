using System.Windows.Controls;
using HillsCafeManagement.ViewModels;

namespace HillsCafeManagement.Views.Employee.Attendance
{
    public partial class AttendanceView : UserControl
    {
        public AttendanceView()
        {
            InitializeComponent();
            // Bind to the updated VM that includes list + filters + refresh
            DataContext = new AttendanceEmployeeViewModel();
        }
    }
}
