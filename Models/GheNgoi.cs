using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaManagement.Models
{
    public class GheNgoi
    {
        [Key]
        [StringLength(10)]
        public string MaGhe { get; set; } = string.Empty;

        [StringLength(30)]
        public string SoGhe { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,2)")]
        public decimal GiaGhe { get; set; }

        [StringLength(50)]
        public string LoaiGhe { get; set; } = string.Empty;

        [StringLength(20)]
        public string TrangThai { get; set; } = string.Empty;

        [StringLength(10)]
        public string MaPhong { get; set; } = string.Empty;

        // Navigation properties
        [ForeignKey("MaPhong")]
        public virtual PhongChieu PhongChieu { get; set; } = null!;
        public virtual ICollection<Ve> Ves { get; set; } = new List<Ve>();
    }
}
