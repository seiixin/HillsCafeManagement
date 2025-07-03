using HillsCafeManagement.Models;
using HillsCafeManagement.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace HillsCafeManagement.ViewModels
{
    public class InventoryViewModel : INotifyPropertyChanged
    {
        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged(nameof(SearchText));
                    FilterInventory();
                }
            }
        }

        public ObservableCollection<InventoryItem> InventoryItems { get; set; } = new();
        public ObservableCollection<InventoryItem> FilteredItems { get; set; } = new();

        public InventoryViewModel()
        {
            LoadData();
        }

        private void LoadData()
        {
            var dbService = new DatabaseService();
            var items = dbService.GetInventoryItems();

            InventoryItems = new ObservableCollection<InventoryItem>(items);
            FilteredItems = new ObservableCollection<InventoryItem>(items);

            OnPropertyChanged(nameof(InventoryItems));
            OnPropertyChanged(nameof(FilteredItems));
        }

        private void FilterInventory()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                FilteredItems = new ObservableCollection<InventoryItem>(InventoryItems);
            }
            else
            {
                var filtered = InventoryItems
                    .Where(item =>
                        (!string.IsNullOrEmpty(item.ProductName) && item.ProductName.Contains(SearchText, System.StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(item.CategoryName) && item.CategoryName.Contains(SearchText, System.StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                FilteredItems = new ObservableCollection<InventoryItem>(filtered);
            }

            OnPropertyChanged(nameof(FilteredItems));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
