using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LinhKienDienTu_Web.Models
{
    public class GiftRule
    {
        [Key]
        public int GiftRule_ID { get; set; }

        [Required]
        public int MainProduct_ID { get; set; }

        [ForeignKey("MainProduct_ID")]
        public Product? MainProduct { get; set; }

        public int MinQuantity { get; set; } = 1;

        [Required]
        public int GiftProduct_ID { get; set; }

        [ForeignKey("GiftProduct_ID")]
        public Product? GiftProduct { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
