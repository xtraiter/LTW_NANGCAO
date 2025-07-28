using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaManagement.Models
{
    [Table("Voucher")]
    public class Voucher
    {
        [Key]
        [Column("maGiamGia")]
        [StringLength(50)]
        public string MaGiamGia { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên khuyến mãi là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên khuyến mãi không được vượt quá 100 ký tự")]
        [Column("tenGiamGia", TypeName = "nvarchar(100)")]
        public string TenGiamGia { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phần trăm giảm là bắt buộc")]
        [Range(1, 100, ErrorMessage = "Phần trăm giảm phải từ 1 đến 100")]
        [Column("phanTramGiam")]
        public int PhanTramGiam { get; set; }

        [Column("moTa", TypeName = "nvarchar(max)")]
        public string MoTa { get; set; } = string.Empty;

        [Required(ErrorMessage = "Thời gian bắt đầu là bắt buộc")]
        [Column("thoiGianBatDau")]
        public DateTime ThoiGianBatDau { get; set; }

        [Required(ErrorMessage = "Thời gian kết thúc là bắt buộc")]
        [Column("thoiGianKetThuc")]
        public DateTime ThoiGianKetThuc { get; set; }

        [Required(ErrorMessage = "Nhân viên tạo là bắt buộc")]
        [StringLength(50)]
        [Column("maNhanVien")]
        public string MaNhanVien { get; set; } = string.Empty;

        // Navigation properties
        [ForeignKey("MaNhanVien")]
        public virtual NhanVien? NhanVien { get; set; }
        public virtual ICollection<HDVoucher> HDVouchers { get; set; } = new List<HDVoucher>();
    }
}
