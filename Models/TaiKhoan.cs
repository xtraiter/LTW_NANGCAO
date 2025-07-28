using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaManagement.Models
{
    public class TaiKhoan
    {
        [Key]
        [StringLength(50)]
        public string MaTK { get; set; } = string.Empty;

        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [StringLength(255)]
        public string MatKhau { get; set; } = string.Empty;

        [StringLength(50)]
        public string Role { get; set; } = string.Empty;

        [StringLength(20)]
        public string TrangThai { get; set; } = string.Empty;

        [StringLength(50)]
        public string? MaNhanVien { get; set; }

        [StringLength(50)]
        public string? MaKhachHang { get; set; }

        // Navigation properties
        public virtual NhanVien? NhanVien { get; set; }
        public virtual KhachHang? KhachHang { get; set; }
    }
}
