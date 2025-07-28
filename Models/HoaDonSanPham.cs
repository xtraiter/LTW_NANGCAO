using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaManagement.Models
{
    public class HoaDonSanPham
    {
        [Key]
        [StringLength(50)]
        public string MaHoaDonSanPham { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,2)")]
        public decimal TongTien { get; set; }

        public DateTime ThoiGianTao { get; set; }

        public int SoLuong { get; set; }

        [StringLength(50)]
        public string MaKhachHang { get; set; } = string.Empty;

        [StringLength(255)]
        public string? DiaChiGiaoHang { get; set; }

        [StringLength(50)]
        public string TrangThai { get; set; } = string.Empty;

        // Navigation properties
        [ForeignKey("MaKhachHang")]
        public virtual KhachHang? KhachHang { get; set; }

        public virtual ICollection<ChiTietHoaDonSanPham> ChiTietHoaDonSanPhams { get; set; } = new List<ChiTietHoaDonSanPham>();
        public virtual ICollection<HoaDonSanPhamVoucher> HoaDonSanPhamVouchers { get; set; } = new List<HoaDonSanPhamVoucher>();
        public virtual ICollection<YeuCauHoanTra> YeuCauHoanTras { get; set; } = new List<YeuCauHoanTra>();
    }
} 