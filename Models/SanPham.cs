using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaManagement.Models
{
    public class SanPham
    {
        [Key]
        [StringLength(50)]
        public string MaSanPham { get; set; } = string.Empty;

        [StringLength(255)]
        public string? TenSanPham { get; set; }

        public string? MoTa { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Gia { get; set; }

        public int SoLuongTon { get; set; }

        [StringLength(255)]
        public string? HinhAnh { get; set; }

        [StringLength(20)]
        public string? TrangThai { get; set; }

        [StringLength(50)]
        public string? MaNhanVien { get; set; }

        // Navigation properties
        [ForeignKey("MaNhanVien")]
        public virtual NhanVien? NhanVien { get; set; }

        public virtual ICollection<ChiTietGioHang> ChiTietGioHangs { get; set; } = new List<ChiTietGioHang>();
        public virtual ICollection<ChiTietHoaDonSanPham> ChiTietHoaDonSanPhams { get; set; } = new List<ChiTietHoaDonSanPham>();
    }
} 