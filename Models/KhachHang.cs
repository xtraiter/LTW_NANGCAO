using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaManagement.Models
{
    public class KhachHang
    {
        [Key]
        [StringLength(50)]
        public string MaKhachHang { get; set; } = string.Empty;

        [StringLength(100)]
        public string HoTen { get; set; } = string.Empty;

        [StringLength(15)]
        public string SDT { get; set; } = string.Empty;

        public int DiemTichLuy { get; set; }

        [StringLength(255)]
        public string? DiaChiGiaoHang { get; set; }

        // Navigation properties
        public virtual ICollection<TaiKhoan> TaiKhoans { get; set; } = new List<TaiKhoan>();
        public virtual ICollection<HoaDon> HoaDons { get; set; } = new List<HoaDon>();
        public virtual ICollection<DanhGiaPhim> DanhGiaPhims { get; set; } = new List<DanhGiaPhim>();
        public virtual ICollection<GioHang> GioHangs { get; set; } = new List<GioHang>();
        public virtual ICollection<HoaDonSanPham> HoaDonSanPhams { get; set; } = new List<HoaDonSanPham>();
        public virtual ICollection<YeuCauHoanTra> YeuCauHoanTras { get; set; } = new List<YeuCauHoanTra>();
        public virtual ICollection<TinNhan> TinNhans { get; set; } = new List<TinNhan>();
    }
}
