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
}
