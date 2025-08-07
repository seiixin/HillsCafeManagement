﻿using System;

namespace HillsCafeManagement.Models
{
    public class MenuModel
    {
        public int Id { get; set; }
        public string? Name { get; set; } = string.Empty;
        public string? Category { get; set; } = string.Empty;
        public decimal? Price { get; set; }
        public string? ImageUrl { get; set; } = string.Empty;
        public string? Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}