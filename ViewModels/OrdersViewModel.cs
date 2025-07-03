using HillsCafeManagement.Models;
using HillsCafeManagement.Services;
using System.Collections.ObjectModel;

namespace HillsCafeManagement.ViewModels
{
    public class OrdersViewModel : BaseViewModel
    {
        private readonly OrderService _orderService;

        public ObservableCollection<OrderModel> Orders { get; set; }

        public OrdersViewModel()
        {
            _orderService = new OrderService();
            LoadOrders();
        }

        public void LoadOrders()
        {
            var list = _orderService.GetAllOrders();
            Orders = new ObservableCollection<OrderModel>(list);
            OnPropertyChanged(nameof(Orders));
        }
    }
}
