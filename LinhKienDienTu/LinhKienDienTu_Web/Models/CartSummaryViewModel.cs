namespace LinhKienDienTu_Web.Models
{
    public class CartSummaryViewModel
    {
        public int TotalItems { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalOriginalAmount { get; set; }
        public decimal TotalPromotionDiscount { get; set; }
        public bool HasTphcmOnlyItems { get; set; }
    }
}
