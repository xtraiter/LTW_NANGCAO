using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaManagement.Models
{
    public class Phim
    {
        [Key]
        [StringLength(50)]
        public string MaPhim { get; set; } = string.Empty;

        [StringLength(255)]
        public string TenPhim { get; set; } = string.Empty;

        [StringLength(100)]
        public string TheLoai { get; set; } = string.Empty;

        public int ThoiLuong { get; set; } // đơn vị phút

        [StringLength(50)]
        public string DoTuoiPhanAnh { get; set; } = string.Empty;

        [Column(TypeName = "nvarchar(max)")]
        public string MoTa { get; set; } = string.Empty;

        [StringLength(255)]
        public string ViTriFilePhim { get; set; } = string.Empty;

        [StringLength(50)]
        public string MaNhanVien { get; set; } = string.Empty;

        // Navigation properties
        [ForeignKey("MaNhanVien")]
        public virtual NhanVien NhanVien { get; set; } = null!;
        public virtual ICollection<LichChieu> LichChieus { get; set; } = new List<LichChieu>();
        public virtual ICollection<Ve> Ves { get; set; } = new List<Ve>();
        public virtual ICollection<DanhGiaPhim> DanhGiaPhims { get; set; } = new List<DanhGiaPhim>();
    }
}
