using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaManagement.Models
{
    public class NhanVien
    {
        [Key]
        [StringLength(50)]
        public string MaNhanVien { get; set; } = string.Empty;

        [StringLength(100)]
        public string TenNhanVien { get; set; } = string.Empty;

        [StringLength(50)]
        public string ChucVu { get; set; } = string.Empty;

        [StringLength(15)]
        public string SDT { get; set; } = string.Empty;

        public DateTime NgaySinh { get; set; }

        // Navigation properties
        public virtual ICollection<PhongChieu> PhongChieus { get; set; } = new List<PhongChieu>();
        public virtual ICollection<TaiKhoan> TaiKhoans { get; set; } = new List<TaiKhoan>();
        public virtual ICollection<Phim> Phims { get; set; } = new List<Phim>();
        public virtual ICollection<LichChieu> LichChieus { get; set; } = new List<LichChieu>();
        public virtual ICollection<HoaDon> HoaDons { get; set; } = new List<HoaDon>();
        public virtual ICollection<Voucher> Vouchers { get; set; } = new List<Voucher>();
    }
}
