using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HillsCafeManagement
{
    public partial class MainWindow : Window
    {
        private const string EmailPlaceholder = "Email";
        private static readonly Brush PlaceholderBrush = Brushes.Gray;
        private static readonly Brush InputBrush = Brushes.Black;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Input_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.Text == EmailPlaceholder)
            {
                textBox.Text = string.Empty;
                textBox.Foreground = InputBrush;
            }
        }

        private void Input_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = EmailPlaceholder;
                textBox.Foreground = PlaceholderBrush;
            }
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            PasswordPlaceholder.Visibility =
                string.IsNullOrEmpty(PasswordBox.Password) ? Visibility.Visible : Visibility.Hidden;
        }
    }
}