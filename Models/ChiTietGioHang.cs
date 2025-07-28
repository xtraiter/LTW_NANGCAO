using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaManagement.Models
{
    public class ChiTietGioHang
    {
        [Key]
        [StringLength(50)]
        public string MaChiTietGioHang { get; set; } = string.Empty;

        [StringLength(50)]
        public string MaGioHang { get; set; } = string.Empty;

        [StringLength(50)]
        public string MaSanPham { get; set; } = string.Empty;

        public int SoLuong { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal DonGia { get; set; }

        // Navigation properties
        [ForeignKey("MaGioHang")]
        public virtual GioHang? GioHang { get; set; }

        [ForeignKey("MaSanPham")]
        public virtual SanPham? SanPham { get; set; }
    }
} 