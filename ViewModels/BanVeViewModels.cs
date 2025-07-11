using CinemaManagement.Models;

namespace CinemaManagement.ViewModels
{
    public class BanVeViewModel
    {
        public List<Phim> DanhSachPhim { get; set; } = new List<Phim>();
        public List<LichChieu> DanhSachLichChieu { get; set; } = new List<LichChieu>();
        public string? PhimDuocChon { get; set; }
        public string? LichChieuDuocChon { get; set; }
        public List<GheNgoi> DanhSachGhe { get; set; } = new List<GheNgoi>();
        public List<string> GheDuocChon { get; set; } = new List<string>();
        public decimal TongTien { get; set; }
        public string? MaKhachHang { get; set; }
        public KhachHang? KhachHang { get; set; }
        public List<Voucher> DanhSachVoucher { get; set; } = new List<Voucher>();
        public string? VoucherDuocChon { get; set; }
        public decimal TienGiamGia { get; set; }
        public decimal ThanhTien { get; set; }
    }

    public class ChonGheViewModel
    {
        public LichChieu LichChieu { get; set; } = new LichChieu();
        public List<GheNgoi> DanhSachGhe { get; set; } = new List<GheNgoi>();
        public List<Ve> DanhSachVeDaBan { get; set; } = new List<Ve>();
        public List<Ve> DanhSachVeDaPhatHanh { get; set; } = new List<Ve>();
        public List<string> GheDuocChon { get; set; } = new List<string>();
        public decimal TongTien { get; set; }
    }

    public class ThanhToanViewModel
    {
        public LichChieu LichChieu { get; set; } = new LichChieu();
        public List<GheNgoi> DanhSachGheDuocChon { get; set; } = new List<GheNgoi>();
        public decimal TongTien { get; set; }
        public string? MaKhachHang { get; set; }
        public KhachHang? KhachHang { get; set; }
        public List<Voucher> DanhSachVoucherKhaDung { get; set; } = new List<Voucher>();
        public string? VoucherDuocChon { get; set; }
        public decimal TienGiamGia { get; set; }
        public decimal ThanhTien { get; set; }
    }

    public class HoaDonViewModel
    {
        public HoaDon HoaDon { get; set; } = new HoaDon();
        public List<CTHD> ChiTietHoaDon { get; set; } = new List<CTHD>();
        public KhachHang? KhachHang { get; set; }
        public NhanVien NhanVien { get; set; } = new NhanVien();
        public Voucher? VoucherSuDung { get; set; }
        public decimal TienGiamGia { get; set; }
    }
}
