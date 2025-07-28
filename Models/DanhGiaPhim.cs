using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaManagement.Models
{
    [Table("DanhGiaPhim")]
    public class DanhGiaPhim
    {
        [Key]
        [Column("maDanhGia")]
        [StringLength(50)]
        public string MaDanhGia { get; set; } = string.Empty;

        [Required(ErrorMessage = "Điểm đánh giá là bắt buộc")]
        [Range(1, 10, ErrorMessage = "Điểm đánh giá phải từ 1 đến 10")]
        [Column("diemDanhGia")]
        public int DiemDanhGia { get; set; }

        [Column("noiDungDanhGia", TypeName = "nvarchar(max)")]
        public string NoiDungDanhGia { get; set; } = string.Empty;

        [Required(ErrorMessage = "Thời gian đánh giá là bắt buộc")]
        [Column("thoiGianDanhGia")]
        public DateTime ThoiGianDanhGia { get; set; }

        [Required(ErrorMessage = "Mã khách hàng là bắt buộc")]
        [StringLength(50)]
        [Column("maKhachHang")]
        public string MaKhachHang { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mã phim là bắt buộc")]
        [StringLength(50)]
        [Column("maPhim")]
        public string MaPhim { get; set; } = string.Empty;

        // Navigation properties
        [ForeignKey("MaKhachHang")]
        public virtual KhachHang KhachHang { get; set; } = null!;

        [ForeignKey("MaPhim")]
        public virtual Phim Phim { get; set; } = null!;
    }
}
