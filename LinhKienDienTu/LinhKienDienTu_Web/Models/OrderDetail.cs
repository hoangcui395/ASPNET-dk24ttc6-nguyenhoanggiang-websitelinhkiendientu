using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LinhKienDienTu_Web.Models
{
    public class OrderDetail
    {
        [Key]
        [Column("Order_Detail_ID")]
        public int OrderDetail_ID { get; set; }

        [Column("Order_ID")]
        public int Order_ID { get; set; }
        public Order? Order { get; set; }

        [Column("Product_ID")]
        public int Product_ID { get; set; }
        public Product? Product { get; set; }

        [Column("So_Luong")]
        public int So_Luong { get; set; }

        /// <summary>Đơn giá tại thời điểm mua — ánh xạ tới cột Don_Gia trong DB</summary>
        [Column("Don_Gia")]
        public decimal Gia_Ban { get; set; }

        /// <summary>Cột computed trong SQL: Thanh_Tien = Don_Gia * So_Luong — EF chỉ đọc, không ghi</summary>
        [Column("Thanh_Tien")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public decimal? Thanh_Tien { get; set; }

        /// <summary>Tính toán phía ứng dụng (không map DB)</summary>
        [NotMapped]
        public decimal LineTotal => Gia_Ban * So_Luong;
    }
}
