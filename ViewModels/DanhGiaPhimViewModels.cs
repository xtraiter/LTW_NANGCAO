using System.ComponentModel.DataAnnotations;

namespace CinemaManagement.ViewModels
{
    public class DanhGiaPhimViewModel
    {
        [Required(ErrorMessage = "Điểm đánh giá là bắt buộc")]
        [Range(1, 10, ErrorMessage = "Điểm đánh giá phải từ 1 đến 10")]
        public int DiemDanhGia { get; set; }

        [StringLength(1000, ErrorMessage = "Nội dung đánh giá không được vượt quá 1000 ký tự")]
        public string NoiDungDanhGia { get; set; } = string.Empty;

        public string MaPhim { get; set; } = string.Empty;
    }

    public class PhimRatingViewModel
    {
        public string MaPhim { get; set; } = string.Empty;
        public string TenPhim { get; set; } = string.Empty;
        public double DiemTrungBinh { get; set; }
        public int TongSoDanhGia { get; set; }
        public List<DanhGiaChiTietViewModel> DanhSachDanhGia { get; set; } = new List<DanhGiaChiTietViewModel>();
        public bool DaDanhGia { get; set; } = false;
        public DanhGiaPhimViewModel? DanhGiaCuaToi { get; set; }
    }

    public class DanhGiaChiTietViewModel
    {
        public string MaDanhGia { get; set; } = string.Empty;
        public int DiemDanhGia { get; set; }
        public string NoiDungDanhGia { get; set; } = string.Empty;
        public DateTime ThoiGianDanhGia { get; set; }
        public string TenKhachHang { get; set; } = string.Empty;
    }
}
