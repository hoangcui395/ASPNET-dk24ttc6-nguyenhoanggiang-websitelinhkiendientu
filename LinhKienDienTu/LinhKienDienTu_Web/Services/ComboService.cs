using Microsoft.EntityFrameworkCore;
using LinhKienDienTu_Web.Models;

namespace LinhKienDienTu_Web.Services
{
    public class ComboService
    {
        private readonly ApplicationDbContext _context;

        public ComboService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Tính toán tồn kho động cho một Combo dựa trên tồn kho của các sản phẩm thành phần.
        /// </summary>
        public async Task<int> CalculateComboStockAsync(int comboId)
        {
            var components = await _context.ComboDetails
                .Include(c => c.ComponentProduct)
                .Where(c => c.Combo_ID == comboId)
                .ToListAsync();

            if (!components.Any()) return 0;

            int minStock = int.MaxValue;

            foreach (var item in components)
            {
                if (item.ComponentProduct == null) continue;

                int possibleCombos = item.ComponentProduct.So_Luong_Ton / item.So_Luong_Thanh_Phan;
                if (possibleCombos < minStock)
                {
                    minStock = possibleCombos;
                }
            }

            return minStock == int.MaxValue ? 0 : minStock;
        }

        /// <summary>
        /// Đồng bộ hóa trạng thái Combo (IsCombo) và các thành phần.
        /// </summary>
        public async Task UpdateComboDetailsAsync(int comboId, List<ComboDetail> newDetails)
        {
            var existingDetails = await _context.ComboDetails
                .Where(cd => cd.Combo_ID == comboId)
                .ToListAsync();

            _context.ComboDetails.RemoveRange(existingDetails);
            
            if (newDetails != null && newDetails.Any())
            {
                foreach (var detail in newDetails)
                {
                    detail.Combo_ID = comboId;
                }
                _context.ComboDetails.AddRange(newDetails);
            }

            await _context.SaveChangesAsync();
        }
    }
}
