using HillsCafeManagement.Models;
using HillsCafeManagement.Services;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace HillsCafeManagement.Views.Admin.Users
{
    public partial class Users : UserControl
    {
        private readonly DatabaseService _dbService = new();
        private List<UserModel> _allUsers = new();

        public Users()
        {
            InitializeComponent();
            LoadUsers();
        }

        private void LoadUsers()
        {
            _allUsers = _dbService.GetAllUsers();
            UserDataGrid.ItemsSource = _allUsers;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string search = SearchBox.Text.Trim().ToLower();

            var filtered = _allUsers.Where(u =>
                (u.Email?.ToLower().Contains(search) ?? false) ||
                (u.Role?.ToLower().Contains(search) ?? false) ||
                (u.Employee?.FullName?.ToLower().Contains(search) ?? false)
            ).ToList();

            UserDataGrid.ItemsSource = filtered;
        }

        private void AddUser_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Open Add User Form here.");
        }

        private void EditUser_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is UserModel user)
            {
                MessageBox.Show($"Edit user: {user.Email}");
            }
        }

        private void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is UserModel user)
            {
                var confirm = MessageBox.Show($"Delete {user.Email}?", "Confirm", MessageBoxButton.YesNo);
                if (confirm == MessageBoxResult.Yes)
                {
                    if (_dbService.DeleteUserById(user.Id))
                        LoadUsers();
                    else
                        MessageBox.Show("Failed to delete.");
                }
            }
        }
    }
}
