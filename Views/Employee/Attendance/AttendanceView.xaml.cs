using System.Windows.Controls;
using HillsCafeManagement.ViewModels;

namespace HillsCafeManagement.Views.Employee.Attendance
{
    public partial class AttendanceView : UserControl
    {
        public AttendanceView()
        {
            InitializeComponent();
            DataContext = new AttendanceEmployeeViewModel(); // <-- important
        }
    }
}
