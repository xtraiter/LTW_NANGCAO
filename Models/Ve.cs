using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaManagement.Models
{
    public class Ve
    {
        [Key]
        [StringLength(50)]
        public string MaVe { get; set; } = string.Empty;

        [StringLength(20)]
        public string TrangThai { get; set; } = string.Empty;

        [StringLength(50)]
        public string SoGhe { get; set; } = string.Empty;

        [StringLength(255)]
        public string TenPhim { get; set; } = string.Empty;

        public DateTime HanSuDung { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Gia { get; set; }

        [StringLength(50)]
        public string TenPhong { get; set; } = string.Empty;

        [StringLength(50)]
        public string MaGhe { get; set; } = string.Empty;

        [StringLength(50)]
        public string MaLichChieu { get; set; } = string.Empty;

        [StringLength(50)]
        public string MaPhim { get; set; } = string.Empty;

        [StringLength(50)]
        public string MaPhong { get; set; } = string.Empty;

        // Navigation properties
        [ForeignKey("MaGhe")]
        public virtual GheNgoi GheNgoi { get; set; } = null!;

        [ForeignKey("MaLichChieu")]
        public virtual LichChieu LichChieu { get; set; } = null!;

        [ForeignKey("MaPhim")]
        public virtual Phim Phim { get; set; } = null!;

        [ForeignKey("MaPhong")]
        public virtual PhongChieu PhongChieu { get; set; } = null!;

        public virtual ICollection<CTHD> CTHDs { get; set; } = new List<CTHD>();
    }
}
