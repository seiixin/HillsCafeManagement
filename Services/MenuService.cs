// File: Services/MenuService.cs
using System.Collections.Generic;
using HillsCafeManagement.Models;

namespace HillsCafeManagement.Services
{
    public class MenuService
    {
        public List<MenuModel> GetAllMenuItems()
        {
            // Example static data for testing
            return new List<MenuModel>
            {
                new MenuModel { Id = 1, Name = "Latte", Category = "Beverage", Price = 120 },
                new MenuModel { Id = 2, Name = "Cheesecake", Category = "Dessert", Price = 150 },
            };
        }
    }
}
