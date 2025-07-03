using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace HillsCafeManagement.Views.Admin.Menu
{
    public partial class MenuView : UserControl
    {
        private List<ProductModel> _products;

        public MenuView()
        {
            InitializeComponent();
            _products = new List<ProductModel>
            {
                new ProductModel{ Name="Mocha", Category="Beverages", Price=240, Description="Espresso, milk, chocolate syrup", ImageUrl="https://..." },
                new ProductModel{ Name="Espresso", Category="Beverages", Price=210, Description="Strong coffee with crema", ImageUrl="https://..." }
            };
            MenuDataGrid.ItemsSource = _products;
        }

        private void SaveProduct_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("TODO: Save product clicked.");
        }

        private void EditProduct_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("TODO: Edit product clicked.");
        }

        private void DeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("TODO: Delete product clicked.");
        }
    }

    public class ProductModel
    {
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public double Price { get; set; }
        public string Description { get; set; } = "";
        public string ImageUrl { get; set; } = "";
    }
}
