using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaManagement.Models
{
    public class HoaDon
    {
        [Key]
        [StringLength(50)]
        public string MaHoaDon { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,2)")]
        public decimal TongTien { get; set; }

        public DateTime ThoiGianTao { get; set; }

        public int SoLuong { get; set; }

        [StringLength(50)]
        public string MaKhachHang { get; set; } = string.Empty;

        [StringLength(50)]
        public string MaNhanVien { get; set; } = string.Empty;

        // Navigation properties
        [ForeignKey("MaKhachHang")]
        public virtual KhachHang? KhachHang { get; set; }

        [ForeignKey("MaNhanVien")]
        public virtual NhanVien NhanVien { get; set; } = null!;

        public virtual ICollection<CTHD> CTHDs { get; set; } = new List<CTHD>();
        public virtual ICollection<HDVoucher> HDVouchers { get; set; } = new List<HDVoucher>();
    }
}
