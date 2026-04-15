using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LinhKienDienTu_Web.Models
{
    public class SubCategory
    {
        [Key]
        public int SubCategory_ID { get; set; }
        [Required(ErrorMessage = "Tên danh mục con không được để trống.")]
        public string Ten_Danh_Muc_Con { get; set; }
        public int Category_ID { get; set; }
        [ForeignKey("Category_ID")]   // 
        public Category? Category { get; set; }
        public List<Product> Products { get; set; } = new();
    }
}
