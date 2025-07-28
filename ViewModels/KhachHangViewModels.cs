using CinemaManagement.Models;
using System.ComponentModel.DataAnnotations;

namespace CinemaManagement.ViewModels
{
    public class GioHangItem
    {
        public string MaLichChieu { get; set; } = string.Empty;
        public string MaGhe { get; set; } = string.Empty;
        public string TenPhim { get; set; } = string.Empty;
        public string TenPhong { get; set; } = string.Empty;
        public string PhongChieu { get; set; } = string.Empty; // Thêm property này
        public string SoGhe { get; set; } = string.Empty;
        public DateTime ThoiGianChieu { get; set; }
        public decimal Gia { get; set; }
    }

    public class KhachHangChonGheViewModel
    {
        public LichChieu LichChieu { get; set; } = null!;
        public List<GheNgoi> DanhSachGhe { get; set; } = new List<GheNgoi>();
        public List<Ve> DanhSachVeDaBan { get; set; } = new List<Ve>();
        public List<Ve> DanhSachVeDaPhatHanh { get; set; } = new List<Ve>();
        public List<string> GheDaDat { get; set; } = new List<string>();
    }

    public class KhachHangThanhToanViewModel
    {
        public List<GioHangItem> GioHang { get; set; } = new List<GioHangItem>();
        public List<Voucher> Vouchers { get; set; } = new List<Voucher>();
        public decimal TongTien { get; set; }
        public string? MaVoucherChon { get; set; }
        public bool IsDirectPayment { get; set; } = false; // Thêm flag cho thanh toán trực tiếp
        public int DiemTichLuy { get; set; } // Điểm tích lũy của khách hàng
    }

    public class ThanhToanThanhCongViewModel
    {
        public HoaDon HoaDon { get; set; } = new HoaDon();
        public List<VeChiTietViewModel> ChiTietVe { get; set; } = new List<VeChiTietViewModel>();
        public Voucher? VoucherSuDung { get; set; }
        public decimal TienGiamGia { get; set; }
        public KhachHang? KhachHang { get; set; }
        public int DiemTichLuyNhan { get; set; }
    }

    public class VeChiTietViewModel
    {
        public string MaVe { get; set; } = string.Empty;
        public string TenPhim { get; set; } = string.Empty;
        public string TenPhong { get; set; } = string.Empty;
        public string SoGhe { get; set; } = string.Empty;
        public DateTime ThoiGianChieu { get; set; }
        public DateTime HanSuDung { get; set; }
        public decimal Gia { get; set; }
        public string TrangThai { get; set; } = string.Empty;
    }

    public class KhachHangViewModels
    {
        public class PhimViewModel
        {
            public List<Phim> Phims { get; set; } = new List<Phim>();
            public List<string> TheLoais { get; set; } = new List<string>();
            public string? CurrentTheLoai { get; set; }
            public string? CurrentSearch { get; set; }
        }

        public class LichSuDatVeViewModel
        {
            public List<HoaDon> LichSuHoaDons { get; set; } = new List<HoaDon>();
        }
    }

    public class SelectedSeatViewModel
    {
        public string MaGhe { get; set; } = string.Empty;
        public string SoGhe { get; set; } = string.Empty;
        public decimal GiaGhe { get; set; }
        public string LoaiGhe { get; set; } = string.Empty;
    }

    public class KhachHangProfileViewModel
    {
        public KhachHang KhachHang { get; set; } = null!;
        public TaiKhoan TaiKhoan { get; set; } = null!;
        public decimal TongChiTieu { get; set; }
        public List<HoaDon> LichSuGanDay { get; set; } = new List<HoaDon>();
        public bool IsGoogleAccount { get; set; } = false;
    }

    public class UpdateProfileViewModel
    {
        [Required(ErrorMessage = "Họ tên không được để trống")]
        [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự")]
        public string HoTen { get; set; } = string.Empty;

        [StringLength(15, ErrorMessage = "Số điện thoại không được vượt quá 15 ký tự")]
        [RegularExpression(@"^[0-9]{10,11}$", ErrorMessage = "Số điện thoại không hợp lệ (10-11 chữ số)")]
        public string SDT { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [StringLength(255, ErrorMessage = "Địa chỉ giao hàng không được vượt quá 255 ký tự")]
        public string? DiaChiGiaoHang { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Mật khẩu hiện tại không được để trống")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu mới không được để trống")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Xác nhận mật khẩu không được để trống")]
        [Compare("NewPassword", ErrorMessage = "Xác nhận mật khẩu không khớp")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
