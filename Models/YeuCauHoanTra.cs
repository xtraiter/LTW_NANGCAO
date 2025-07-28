using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaManagement.Models
{
    public class YeuCauHoanTra
    {
        [Key]
        [StringLength(50)]
        public string MaYeuCau { get; set; } = string.Empty;

        [StringLength(50)]
        public string MaHoaDonSanPham { get; set; } = string.Empty;

        [StringLength(50)]
        public string MaKhachHang { get; set; } = string.Empty;

        public string? LyDo { get; set; }

        public DateTime ThoiGianYeuCau { get; set; }

        [StringLength(20)]
        public string TrangThai { get; set; } = string.Empty;

        // Navigation properties
        [ForeignKey("MaHoaDonSanPham")]
        public virtual HoaDonSanPham? HoaDonSanPham { get; set; }

        [ForeignKey("MaKhachHang")]
        public virtual KhachHang? KhachHang { get; set; }
    }
} 