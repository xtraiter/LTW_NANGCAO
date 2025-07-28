using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaManagement.Models
{
    public class PhongChieu
    {
        [Key]
        [StringLength(50)]
        public string MaPhong { get; set; } = string.Empty;

        [StringLength(50)]
        public string TenPhong { get; set; } = string.Empty;

        public int SoChoNgoi { get; set; }

        [StringLength(50)]
        public string LoaiPhong { get; set; } = string.Empty;

        [StringLength(50)]
        public string TrangThai { get; set; } = string.Empty;

        [StringLength(50)]
        public string MaNhanVien { get; set; } = string.Empty;

        // Navigation properties
        [ForeignKey("MaNhanVien")]
        public virtual NhanVien NhanVien { get; set; } = null!;
        public virtual ICollection<GheNgoi> GheNgois { get; set; } = new List<GheNgoi>();
        public virtual ICollection<LichChieu> LichChieus { get; set; } = new List<LichChieu>();
        public virtual ICollection<Ve> Ves { get; set; } = new List<Ve>();
    }
}
