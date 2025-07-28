using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaManagement.Models
{
    public class GioHang
    {
        [Key]
        [StringLength(50)]
        public string MaGioHang { get; set; } = string.Empty;

        [StringLength(50)]
        public string MaKhachHang { get; set; } = string.Empty;

        public DateTime ThoiGianTao { get; set; }

        [StringLength(20)]
        public string TrangThai { get; set; } = string.Empty;

        // Navigation properties
        [ForeignKey("MaKhachHang")]
        public virtual KhachHang? KhachHang { get; set; }

        public virtual ICollection<ChiTietGioHang> ChiTietGioHangs { get; set; } = new List<ChiTietGioHang>();
    }
} 