using System.ComponentModel.DataAnnotations;

namespace LinhKienDienTu_Web.Models
{
    public class Category
    {
        [Key]
        public int Category_ID { get; set; }
        [Required(ErrorMessage = "Tên danh mục không được để trống.")]
        public string Ten_Danh_Muc { get; set; }

        public List<SubCategory> SubCategories { get; set; } = new();
    }
}
