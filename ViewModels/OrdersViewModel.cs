using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using HillsCafeManagement.Models;
using HillsCafeManagement.Services;

namespace HillsCafeManagement.ViewModels
{
    public class OrdersViewModel : BaseViewModel
    {
        private readonly OrderService _orderService = new OrderService();

        // Main grid source
        public ObservableCollection<OrderModel> Orders { get; set; } = new();

        // Products for the dropdown (ComboBox) in the items editor
        public ObservableCollection<MenuModel> MenuProducts { get; set; } = new();

        // ----- Inline editor state -----
        public OrderModel? EditingOrder { get; set; }
        public ObservableCollection<OrderItemModel> EditingItems { get; set; } = new();

        // ----- READ-ONLY info state (for ℹ️ overlay) -----
        public OrderModel? InfoOrder { get; set; }
        public ObservableCollection<OrderItemModel> InfoItems { get; set; } = new();

        public OrdersViewModel()
        {
            try { LoadMenu(); } catch { /* ignore */ }
            try { LoadOrders(); } catch { /* ignore */ }
        }

        // ===== Loaders =====
        public void LoadOrders()
        {
            try
            {
                var list = _orderService.GetAllOrders(); // includes Items via service
                Orders = new ObservableCollection<OrderModel>(list);
                OnPropertyChanged(nameof(Orders));
            }
            catch
            {
                Orders = new ObservableCollection<OrderModel>();
                OnPropertyChanged(nameof(Orders));
            }
        }

        public void LoadMenu()
        {
            MenuProducts.Clear();
            var list = _orderService.GetAllMenu();
            foreach (var p in list) MenuProducts.Add(p);
            OnPropertyChanged(nameof(MenuProducts));
        }

        // ===== Editor lifecycle =====
        public void BeginAdd()
        {
            EditingOrder = new OrderModel
            {
                CreatedAt = DateTime.Now,
                PaymentStatus = PaymentStatus.Unpaid,
                OrderStatus = OrderStatus.Pending,
                TableNumber = ""
            };
            EditingItems = new ObservableCollection<OrderItemModel>();
            OnPropertyChanged(nameof(EditingOrder));
            OnPropertyChanged(nameof(EditingItems));
        }

        public void BeginEdit(OrderModel source)
        {
            if (source == null) return;

            // Get the freshest items from DB (safer if grid row didn't include items)
            var items = _orderService.GetOrderItems(source.Id);

            // shallow clone header to avoid mutating the grid row until Save
            EditingOrder = new OrderModel
            {
                Id = source.Id,
                CustomerId = source.CustomerId,
                TableNumber = source.TableNumber,
                TotalAmount = source.TotalAmount,
                PaymentStatus = source.PaymentStatus,
                CreatedAt = source.CreatedAt,
                CashRegisterId = source.CashRegisterId,
                OrderStatus = source.OrderStatus,
                OrderedByUserId = source.OrderedByUserId
            };

            // clone items for editing
            EditingItems = new ObservableCollection<OrderItemModel>(
                (items ?? new List<OrderItemModel>()).Select(it => new OrderItemModel
                {
                    Id = it.Id,
                    OrderId = it.OrderId,
                    ProductId = it.ProductId,
                    Quantity = it.Quantity,
                    UnitPrice = it.UnitPrice,
                    ProductName = it.ProductName,
                    Category = it.Category
                })
            );

            OnPropertyChanged(nameof(EditingOrder));
            OnPropertyChanged(nameof(EditingItems));
        }

        // ===== Info (read-only) =====
        public void BeginInfo(OrderModel source)
        {
            if (source == null) return;

            // Ensure we show up-to-date items
            var items = _orderService.GetOrderItems(source.Id);

            InfoOrder = source; // header from grid/service
            InfoItems = new ObservableCollection<OrderItemModel>(items ?? new List<OrderItemModel>());

            OnPropertyChanged(nameof(InfoOrder));
            OnPropertyChanged(nameof(InfoItems));
        }

        // ===== Item row ops =====
        public void AddLine()
        {
            EditingItems.Add(new OrderItemModel { Quantity = 1 });
            OnPropertyChanged(nameof(EditingItems));
            RecalcEditingTotal();
        }

        public void RemoveLine(OrderItemModel item)
        {
            if (item == null) return;
            EditingItems.Remove(item);
            OnPropertyChanged(nameof(EditingItems));
            RecalcEditingTotal();
        }

        public void RecalcEditingTotal()
        {
            if (EditingOrder == null) return;
            EditingOrder.Items = new List<OrderItemModel>(EditingItems);
            EditingOrder.RecalculateTotal();
            OnPropertyChanged(nameof(EditingOrder));
        }

        // ===== Persist =====
        public void SaveEditing()
        {
            if (EditingOrder == null) return;

            EditingOrder.Items = new List<OrderItemModel>(EditingItems);
            EditingOrder.RecalculateTotal();

            if (EditingOrder.Id == 0)
                _orderService.AddOrder(EditingOrder);
            else
                _orderService.UpdateOrder(EditingOrder);

            LoadOrders();
        }

        public void DeleteOrder(int id)
        {
            _orderService.DeleteOrder(id);
            LoadOrders();
        }
    }
}
