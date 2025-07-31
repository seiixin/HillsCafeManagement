using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace HillsCafeManagement.Views.Cashier.POS
{
    public partial class POSView : UserControl
    {
        public ObservableCollection<Product> ProductList { get; set; }
        public ObservableCollection<CartItem> CartList { get; set; }

        public POSView()
        {
            InitializeComponent();
            LoadDummyData();
            this.DataContext = this;
        }

        private void LoadDummyData()
        {
            ProductList = new ObservableCollection<Product>
            {
                new Product { ProductName = "Espresso", Price = 120 },
                new Product { ProductName = "Iced Latte", Price = 150 }
            };

            CartList = new ObservableCollection<CartItem>
            {
                new CartItem { Product = "Espresso", Qty = 1, Price = 120 },
                new CartItem { Product = "Iced Latte", Qty = 2, Price = 300 }
            };
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }

    public class Product
    {
        public string ProductName { get; set; }
        public double Price { get; set; }
    }

    public class CartItem
    {
        public string Product { get; set; }
        public int Qty { get; set; }
        public double Price { get; set; }
    }
}
