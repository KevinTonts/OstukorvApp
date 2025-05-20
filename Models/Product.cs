using System;
using System.Collections.Generic;

namespace OstukorvApp.Models
{
    public class Product
    {
        public int Id { get; set; }
        public int StoreId { get; set; }
        public Store Store { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string PricePerUnit { get; set; }
        public string Url { get; set; }
        public DateTime ScrapedAt { get; set; } = DateTime.UtcNow;
    }

    public class Store
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<Product> Products { get; set; } = new();
    }
}
