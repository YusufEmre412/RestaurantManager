using System.ComponentModel.DataAnnotations;

namespace RestaurantManager.Models
{
    public class SystemConfig
    {
        [Key]
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
