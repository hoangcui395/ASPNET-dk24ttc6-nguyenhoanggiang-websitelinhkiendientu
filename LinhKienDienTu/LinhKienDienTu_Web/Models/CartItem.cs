using System.ComponentModel.DataAnnotations;

namespace LinhKienDienTu_Web.Models
{
    public class CartItem
    {
        public int ProductId { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal OriginalPrice { get; set; }
        public string Image { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public bool IsTphcmOnly { get; set; }
        public int AvailableStock { get; set; }
        public decimal LineTotal => Price * Quantity;
    }
}
