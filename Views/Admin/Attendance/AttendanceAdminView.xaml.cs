using HillsCafeManagement.ViewModels;
using System.Windows.Controls;

namespace HillsCafeManagement.Views.Admin.Attendance
{
    public partial class AttendanceAdminView : UserControl
    {
        public AttendanceAdminView()
        {
            InitializeComponent();
            DataContext = new AttendanceAdminViewModel();
        }
    }
}