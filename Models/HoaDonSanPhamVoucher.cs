using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaManagement.Models
{
    public class HoaDonSanPhamVoucher
    {
        [StringLength(50)]
        public string MaHoaDonSanPham { get; set; } = string.Empty;

        [StringLength(50)]
        public string MaVoucherSanPham { get; set; } = string.Empty;

        public int SoLuongVoucher { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal TongTienGiam { get; set; }

        // Navigation properties
        [ForeignKey("MaHoaDonSanPham")]
        public virtual HoaDonSanPham? HoaDonSanPham { get; set; }

        [ForeignKey("MaVoucherSanPham")]
        public virtual VoucherSanPham? VoucherSanPham { get; set; }
    }
} 