using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaManagement.Models
{
    public class TinNhan
    {
        [Key]
        [StringLength(50)]
        public string MaTinNhan { get; set; } = string.Empty;

        [StringLength(50)]
        public string MaKhachHang { get; set; } = string.Empty;

        public string? NoiDung { get; set; }

        [StringLength(255)]
        public string? HinhAnh { get; set; }

        public DateTime ThoiGianGui { get; set; } = DateTime.Now;

        [StringLength(20)]
        public string TrangThai { get; set; } = "Đã gửi";

        public string? NoiDungTraNoi { get; set; }

        // Navigation properties
        [ForeignKey("MaKhachHang")]
        public virtual KhachHang? KhachHang { get; set; }
    }
} 