using CinemaManagement.Models;

namespace CinemaManagement.ViewModels
{
    public class GioHangItem
    {
        public string MaLichChieu { get; set; } = string.Empty;
        public string MaGhe { get; set; } = string.Empty;
        public string TenPhim { get; set; } = string.Empty;
        public string TenPhong { get; set; } = string.Empty;
        public string SoGhe { get; set; } = string.Empty;
        public DateTime ThoiGianChieu { get; set; }
        public decimal Gia { get; set; }
    }

    public class ThemVeRequest
    {
        public string MaLichChieu { get; set; } = string.Empty;
        public string MaGhe { get; set; } = string.Empty;
    }

    public class KhachHangChonGheViewModel
    {
        public LichChieu LichChieu { get; set; } = null!;
        public List<string> GheDaDat { get; set; } = new List<string>();
    }

    public class KhachHangThanhToanViewModel
    {
        public List<GioHangItem> GioHang { get; set; } = new List<GioHangItem>();
        public List<Voucher> Vouchers { get; set; } = new List<Voucher>();
        public decimal TongTien { get; set; }
        public string? MaVoucherChon { get; set; }
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
}
