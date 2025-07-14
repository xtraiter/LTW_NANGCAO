using CinemaManagement.Models;

namespace CinemaManagement.ViewModels
{
    public class DashboardViewModel
    {
        // Thống kê tổng quan
        public int TongSoVe { get; set; }
        public int VeHomNay { get; set; }
        public int VeTuanNay { get; set; }
        public int VeThangNay { get; set; }
        
        // Thống kê vé đã bán trong hóa đơn
        public int TongSoVeDaBanTrongHoaDon { get; set; }
        public int VeDaBanTrongHoaDonHomNay { get; set; }

        // Thống kê doanh thu
        public decimal DoanhThuHomNay { get; set; }
        public decimal DoanhThuTuanNay { get; set; }
        public decimal DoanhThuThangNay { get; set; }

        // Thống kê tổng tiền hóa đơn
        public decimal TongTienHoaDon { get; set; }
        public decimal TienHoaDonHomNay { get; set; }

        // Thống kê lịch chiếu
        public int LichChieuHomNay { get; set; }
        public int LichChieuTuanNay { get; set; }

        // Thống kê phim
        public int TongSoPhim { get; set; }
        public int PhimDangChieu { get; set; }

        // Thống kê phòng chiếu
        public int TongSoPhong { get; set; }
        public int TongSoGhe { get; set; }

        // Thống kê vé tổng quan mới
        public int TongVeDaTao { get; set; }
        public int TongVeDaBanTheoHoaDon { get; set; }

        // Top phim bán chạy
        public List<TopPhimViewModel> TopPhimBanChay { get; set; } = new List<TopPhimViewModel>();

        // Lịch chiếu gần nhất
        public List<LichChieu> LichChieuGanNhat { get; set; } = new List<LichChieu>();

        // Doanh thu theo ngày (từ hóa đơn)
        public List<DoanhThuTheoNgayViewModel> DoanhThuTheoNgay { get; set; } = new List<DoanhThuTheoNgayViewModel>();

        // Doanh thu theo tháng
        public List<DoanhThuTheoThangViewModel> DoanhThuTheoThang { get; set; } = new List<DoanhThuTheoThangViewModel>();
    }

    public class TopPhimViewModel
    {
        public string MaPhim { get; set; } = string.Empty;
        public string TenPhim { get; set; } = string.Empty;
        public int SoVe { get; set; }
        public decimal DoanhThu { get; set; }
    }

    public class DoanhThuTheoNgayViewModel
    {
        public DateTime Ngay { get; set; }
        public decimal DoanhThu { get; set; }
        public int SoVe { get; set; }
    }

    public class DoanhThuTheoThangViewModel
    {
        public int Thang { get; set; }
        public int Nam { get; set; }
        public decimal DoanhThu { get; set; }
        public int SoVe { get; set; }
    }

    public class ThongKeChiTietViewModel
    {
        public int TongSoVe { get; set; }
        public decimal TongDoanhThu { get; set; }
        public int TongSoPhim { get; set; }
        public int TongSoLichChieu { get; set; }
        
        public List<ThongKePhimChiTietViewModel> ThongKeTheoPhim { get; set; } = new List<ThongKePhimChiTietViewModel>();
        public List<ThongKePhongViewModel> ThongKeTheoPhong { get; set; } = new List<ThongKePhongViewModel>();
        public List<DoanhThuTheoNgayViewModel> DoanhThuTheoNgay { get; set; } = new List<DoanhThuTheoNgayViewModel>();
        public List<DoanhThuTheoThangViewModel> DoanhThuTheoThang { get; set; } = new List<DoanhThuTheoThangViewModel>();
    }

    public class ThongKePhimChiTietViewModel
    {
        public string MaPhim { get; set; } = string.Empty;
        public string TenPhim { get; set; } = string.Empty;
        public int SoVe { get; set; }
        public decimal DoanhThu { get; set; }
        public decimal GiaTrungBinh { get; set; }
    }

    public class ThongKePhongViewModel
    {
        public string MaPhong { get; set; } = string.Empty;
        public string TenPhong { get; set; } = string.Empty;
        public int SoVe { get; set; }
        public decimal DoanhThu { get; set; }
        public double TiLeLapDay { get; set; }
    }

    public class BaoCaoViewModel
    {
        public decimal TongDoanhThu { get; set; }
        public int TongSoVe { get; set; }
        public int TongSoPhim { get; set; }
        public int TongSoLichChieu { get; set; }
        public Dictionary<string, decimal> DoanhThuTheoPhim { get; set; } = new Dictionary<string, decimal>();
    }
}
