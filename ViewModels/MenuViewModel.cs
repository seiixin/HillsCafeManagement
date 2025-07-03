using System.Collections.ObjectModel;
using HillsCafeManagement.Models;
using HillsCafeManagement.Services;

namespace HillsCafeManagement.ViewModels
{
    public class MenuViewModel : BaseViewModel
    {
        public ObservableCollection<MenuModel> MenuItems { get; set; } = new();

        public void LoadMenuItems()
        {
            var service = new MenuService();
            var items = service.GetAllMenuItems();
            MenuItems = new ObservableCollection<MenuModel>(items);
            OnPropertyChanged(nameof(MenuItems));
        }
    }
}
