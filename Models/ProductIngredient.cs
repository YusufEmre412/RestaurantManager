using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantManager.Models
{
    public class ProductIngredient
    {
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public int IngredientId { get; set; }
        [ForeignKey("IngredientId")]
        public Inventory Inventory { get; set; } = null!;

        public int Quantity { get; set; }
    }
}
