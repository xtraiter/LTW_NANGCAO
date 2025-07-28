using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaManagement.Models
{
    public class CTHD
    {
        [Key]
        [StringLength(50)]
        public string MaCTHD { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,2)")]
        public decimal DonGia { get; set; }

        [StringLength(50)]
        public string MaVe { get; set; } = string.Empty;

        [StringLength(50)]
        public string MaHoaDon { get; set; } = string.Empty;

        // Navigation properties
        [ForeignKey("MaVe")]
        public virtual Ve Ve { get; set; } = null!;

        [ForeignKey("MaHoaDon")]
        public virtual HoaDon HoaDon { get; set; } = null!;
    }
}
