using System.Windows;
using System.Windows.Controls;
using HillsCafeManagement.ViewModels;

namespace HillsCafeManagement.Views.Employee.Profile
{
    public partial class ProfileView : UserControl
    {
        private bool _isEditing;

        // Use this overload if you can pass the logged-in employee id
        public ProfileView(int employeeId)
        {
            InitializeComponent();
            var vm = new EmployeeProfileViewModel { EmployeeId = employeeId };
            DataContext = vm;
            vm.LoadCommand.Execute(null);
        }

        // Parameterless for XAML designer or fallback
        public ProfileView() : this(1) { }

        private void OnEditSaveClicked(object sender, RoutedEventArgs e)
        {
            _isEditing = !_isEditing;
            btnEditSave.Content = _isEditing ? "Save" : "Edit";
            ToggleReadonly(!_isEditing);

            if (!_isEditing && DataContext is EmployeeProfileViewModel vm && vm.SaveCommand.CanExecute(null))
            {
                vm.SaveCommand.Execute(null);
            }
        }

        private void ToggleReadonly(bool readOnly)
        {
            tbFullName.IsReadOnly = readOnly;
            tbAge.IsReadOnly = readOnly;
            tbSex.IsReadOnly = readOnly;
            tbAddress.IsReadOnly = readOnly;
            tbBirthday.IsReadOnly = readOnly;
            tbContact.IsReadOnly = readOnly;
            tbPosition.IsReadOnly = readOnly;
            tbSalary.IsReadOnly = readOnly;

            tbSSS.IsReadOnly = readOnly;
            tbPhilhealth.IsReadOnly = readOnly;
            tbPagibig.IsReadOnly = readOnly;
            tbEmail.IsReadOnly = readOnly;
            tbEmergency.IsReadOnly = readOnly;
            tbHired.IsReadOnly = readOnly;
        }
    }
}
