using CinemaManagement.Models;

namespace CinemaManagement.ViewModels
{
    public class PhatHanhHangLoatViewModel
    {
        public string MaLichChieu { get; set; } = string.Empty;
        public LichChieu LichChieu { get; set; } = null!;
        public List<GheNgoi> DanhSachGhe { get; set; } = new List<GheNgoi>();
        public List<string> GheCoVe { get; set; } = new List<string>();
        public List<string> GheChon { get; set; } = new List<string>();
    }

    public class SoDoGheViewModel
    {
        public LichChieu LichChieu { get; set; } = null!;
        public List<GheNgoi> DanhSachGhe { get; set; } = new List<GheNgoi>();
        public List<string> GheCoVe { get; set; } = new List<string>();
    }

    public class ThongKeVeViewModel
    {
        public int TongSoVe { get; set; }
        public int VeConHan { get; set; }
        public int VeHetHan { get; set; }
        public int VeDaBan { get; set; }
        public decimal TongDoanhThu { get; set; }
        public List<ThongKePhimViewModel> ThongKeTheoPhim { get; set; } = new List<ThongKePhimViewModel>();
    }

    public class ThongKePhimViewModel
    {
        public string TenPhim { get; set; } = string.Empty;
        public int SoVe { get; set; }
        public decimal DoanhThu { get; set; }
    }

    public class SelectListItem
    {
        public string Value { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }

    public class PhatHanhVeIndexViewModel
    {
        public List<LichChieu> LichChieus { get; set; } = new List<LichChieu>();
        public List<SelectListItem> DanhSachPhim { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> DanhSachPhong { get; set; } = new List<SelectListItem>();
        public string? TuNgay { get; set; }
        public string? DenNgay { get; set; }
        public string? MaPhimSelected { get; set; }
        public string? MaPhongSelected { get; set; }
    }
}
