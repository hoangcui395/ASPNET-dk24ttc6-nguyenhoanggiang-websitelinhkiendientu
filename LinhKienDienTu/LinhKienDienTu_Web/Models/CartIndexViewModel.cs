namespace LinhKienDienTu_Web.Models
{
    public class CartIndexViewModel
    {
        public List<CartItem> Items { get; set; } = new();
        public CartSummaryViewModel Summary { get; set; } = new();
    }
}
