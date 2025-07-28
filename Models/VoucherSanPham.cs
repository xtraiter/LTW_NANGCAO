using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaManagement.Models
{
    public class VoucherSanPham
    {
        [Key]
        [StringLength(50)]
        public string MaVoucherSanPham { get; set; } = string.Empty;

        [StringLength(100)]
        public string TenVoucher { get; set; } = string.Empty;

        public int PhanTramGiam { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal GiaTriGiamToiDa { get; set; }

        public string? MoTa { get; set; }

        public DateTime ThoiGianBatDau { get; set; }

        public DateTime ThoiGianKetThuc { get; set; }

        public int SoLuong { get; set; }

        public string? DieuKienApDung { get; set; }

        // Navigation properties
        public virtual ICollection<HoaDonSanPhamVoucher> HoaDonSanPhamVouchers { get; set; } = new List<HoaDonSanPhamVoucher>();
    }
} 