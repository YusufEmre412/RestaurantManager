using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantManager.Models
{
    public class Product
    {
        [Key]
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Cost { get; set; }
        
        public int CategoryId { get; set; }
        public Category? Category { get; set; }
        
        public ProductInfo? ProductInfo { get; set; }
    }

    public class Category
    {
        [Key]
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public List<Product> Products { get; set; } = new();
    }

    public class ProductInfo
    {
        [Key]
        public int ProductInfoId { get; set; }
        public int ProductId { get; set; }
        public Product? Product { get; set; }
        
        // Linking to Inventory Item if this product consumes inventory
        public int? ItemId { get; set; }
        public Inventory? InventoryItem { get; set; }
        
        public string Description { get; set; } = string.Empty;
    }

    public class Inventory
    {
        [Key]
        public int ItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal ItemCost { get; set; }
    }
    
    public class Table
    {
        [Key]
        public int TableId { get; set; }
        public int TableNumber { get; set; }
        public bool IsOccupied { get; set; }
    }
}
