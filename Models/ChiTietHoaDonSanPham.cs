using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaManagement.Models
{
    public class ChiTietHoaDonSanPham
    {
        [Key]
        [StringLength(50)]
        public string MaChiTietHoaDonSanPham { get; set; } = string.Empty;

        [StringLength(50)]
        public string MaHoaDonSanPham { get; set; } = string.Empty;

        [StringLength(50)]
        public string MaSanPham { get; set; } = string.Empty;

        public int SoLuong { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal DonGia { get; set; }

        // Navigation properties
        [ForeignKey("MaHoaDonSanPham")]
        public virtual HoaDonSanPham? HoaDonSanPham { get; set; }

        [ForeignKey("MaSanPham")]
        public virtual SanPham? SanPham { get; set; }
    }
} 