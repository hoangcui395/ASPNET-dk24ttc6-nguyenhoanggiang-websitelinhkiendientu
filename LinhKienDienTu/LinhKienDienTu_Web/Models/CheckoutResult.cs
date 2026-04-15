namespace LinhKienDienTu_Web.Models
{
    public class CheckoutResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? OrderId { get; set; }
        public decimal TotalAmount { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}
