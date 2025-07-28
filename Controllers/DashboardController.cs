using Microsoft.AspNetCore.Mvc;
using CinemaManagement.Data;
using CinemaManagement.Models;
using CinemaManagement.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace CinemaManagement.Controllers
{
    public class DashboardController : Controller
    {
        private readonly CinemaDbContext _context;

        public DashboardController(CinemaDbContext context)
        {
            _context = context;
        }

        private bool IsManager()
        {
            var vaiTro = HttpContext.Session.GetString("VaiTro");
            return vaiTro == "Quản lý";
        }

        private bool IsManagerOrStaff()
        {
            var vaiTro = HttpContext.Session.GetString("VaiTro");
            return vaiTro == "Quản lý" || vaiTro == "Nhân viên";
        }

        public async Task<IActionResult> Index(DateTime? tuNgay, DateTime? denNgay, string? tenPhim)
        {
            if (!IsManagerOrStaff())
            {
                return RedirectToAction("Index", "Home");
            }

            var today = DateTime.Today;
            var thisWeek = today.AddDays(-(int)today.DayOfWeek);
            var thisMonth = new DateTime(today.Year, today.Month, 1);

            // Khởi tạo query gốc
            var veQuery = _context.Ves
                .Include(v => v.LichChieu)
                    .ThenInclude(lc => lc.Phim)
                .AsQueryable();

            // Áp dụng bộ lọc nếu có
            if (tuNgay.HasValue)
                veQuery = veQuery.Where(v => v.HanSuDung.Date >= tuNgay.Value.Date);
            if (denNgay.HasValue)
                veQuery = veQuery.Where(v => v.HanSuDung.Date <= denNgay.Value.Date);
            if (!string.IsNullOrEmpty(tenPhim))
                veQuery = veQuery.Where(v => v.LichChieu.Phim.TenPhim.Contains(tenPhim));

            // Gán vào ViewModel
            var dashboard = new DashboardViewModel
            {
                TuNgay = tuNgay,
                DenNgay = denNgay,
                TenPhim = tenPhim,

                // Thống kê cơ bản
                TongSoVe = await _context.Ves.CountAsync(),
                VeHomNay = await _context.Ves.CountAsync(v => v.HanSuDung.Date == today),
                VeTuanNay = await _context.Ves.CountAsync(v => v.HanSuDung >= thisWeek),
                VeThangNay = await _context.Ves.CountAsync(v => v.HanSuDung >= thisMonth),

                // Thống kê vé đã bán trong hóa đơn
                TongSoVeDaBanTrongHoaDon = await _context.HoaDons.SumAsync(h => h.SoLuong),
                VeDaBanTrongHoaDonHomNay = await _context.HoaDons.Where(h => h.ThoiGianTao.Date == today).SumAsync(h => h.SoLuong),

                // Thống kê doanh thu
                DoanhThuHomNay = await _context.Ves.Where(v => v.HanSuDung.Date == today).SumAsync(v => v.Gia),
                DoanhThuTuanNay = await _context.Ves.Where(v => v.HanSuDung >= thisWeek).SumAsync(v => v.Gia),
                DoanhThuThangNay = await _context.Ves.Where(v => v.HanSuDung >= thisMonth).SumAsync(v => v.Gia),

                // Thống kê tổng tiền hóa đơn
                TongTienHoaDon = await _context.HoaDons.SumAsync(h => h.TongTien),
                TienHoaDonHomNay = await _context.HoaDons.Where(h => h.ThoiGianTao.Date == today).SumAsync(h => h.TongTien),

                // Lịch chiếu (không áp dụng bộ lọc)
                LichChieuHomNay = await _context.LichChieus.CountAsync(l => l.ThoiGianBatDau.Date == today),
                LichChieuTuanNay = await _context.LichChieus.CountAsync(l => l.ThoiGianBatDau >= thisWeek),

                // Phim, phòng, ghế (không lọc)
                TongSoPhim = await _context.Phims.CountAsync(),
                PhimDangChieu = await _context.LichChieus
                    .Where(l => l.ThoiGianBatDau >= DateTime.Now)
                    .Select(l => l.MaPhim)
                    .Distinct()
                    .CountAsync(),
                TongSoPhong = await _context.PhongChieus.CountAsync(),
                TongSoGhe = await _context.GheNgois.CountAsync(),

                // Top phim theo bộ lọc - từ hóa đơn đã bán
                TopPhimBanChay = await _context.CTHDs
                    .Include(c => c.Ve)
                        .ThenInclude(v => v.LichChieu)
                        .ThenInclude(l => l.Phim)
                    .Include(c => c.HoaDon)
                    .Where(c => !tuNgay.HasValue || c.HoaDon.ThoiGianTao.Date >= tuNgay.Value.Date)
                    .Where(c => !denNgay.HasValue || c.HoaDon.ThoiGianTao.Date <= denNgay.Value.Date)
                    .Where(c => string.IsNullOrEmpty(tenPhim) || c.Ve.TenPhim.Contains(tenPhim))
                    .GroupBy(c => new { c.Ve.MaPhim, c.Ve.TenPhim })
                    .Select(g => new TopPhimViewModel
                    {
                        MaPhim = g.Key.MaPhim,
                        TenPhim = g.Key.TenPhim,
                        SoVe = g.Count(),
                        DoanhThu = g.Sum(c => c.DonGia)
                    })
                    .OrderByDescending(t => t.SoVe)
                    .Take(5)
                    .ToListAsync(),

                // Lịch chiếu gần nhất
                LichChieuGanNhat = await _context.LichChieus
                    .Include(l => l.Phim)
                    .Include(l => l.PhongChieu)
                    .Where(l => l.ThoiGianBatDau >= DateTime.Now)
                    .OrderBy(l => l.ThoiGianBatDau)
                    .Take(5)
                    .ToListAsync(),

                // Dữ liệu biểu đồ (chưa áp dụng lọc vì phụ thuộc yêu cầu)
                DoanhThuTheoNgay = await GetDoanhThuHoaDonTheoNgay(7),
                DoanhThuTheoThang = await GetDoanhThuTheoThang(12)
            };

            return View(dashboard);
        }

        [HttpGet]
        public async Task<IActionResult> GetDoanhThuData(string type = "day", int days = 7)
        {
            if (!IsManagerOrStaff())
            {
                return Json(new { success = false, message = "Không có quyền truy cập" });
            }

            try
            {
                if (type == "day")
                {
                    var data = await GetDoanhThuTheoNgay(days);
                    return Json(new { success = true, data = data });
                }
                else if (type == "month")
                {
                    var data = await GetDoanhThuTheoThang(days);
                    return Json(new { success = true, data = data });
                }
                else
                {
                    return Json(new { success = false, message = "Loại dữ liệu không hợp lệ" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private async Task<List<DoanhThuTheoNgayViewModel>> GetDoanhThuTheoNgay(int days)
        {
            var result = new List<DoanhThuTheoNgayViewModel>();
            var startDate = DateTime.Today.AddDays(-days + 1);

            for (int i = 0; i < days; i++)
            {
                var date = startDate.AddDays(i);
                var doanhThu = await _context.Ves
                    .Where(v => v.HanSuDung.Date == date)
                    .SumAsync(v => v.Gia);

                var soVe = await _context.Ves
                    .CountAsync(v => v.HanSuDung.Date == date);

                result.Add(new DoanhThuTheoNgayViewModel
                {
                    Ngay = date,
                    DoanhThu = doanhThu,
                    SoVe = soVe
                });
            }

            return result;
        }

        private async Task<List<DoanhThuTheoNgayViewModel>> GetDoanhThuHoaDonTheoNgay(int days)
        {
            var result = new List<DoanhThuTheoNgayViewModel>();
            var startDate = DateTime.Today.AddDays(-days + 1);

            for (int i = 0; i < days; i++)
            {
                var date = startDate.AddDays(i);
                
                // Lấy doanh thu từ vé đã bán trong hóa đơn
                var doanhThu = await _context.CTHDs
                    .Join(_context.HoaDons, ct => ct.MaHoaDon, hd => hd.MaHoaDon, (ct, hd) => new { ct, hd })
                    .Where(x => x.hd.ThoiGianTao.Date == date)
                    .Join(_context.Ves, x => x.ct.MaVe, v => v.MaVe, (x, v) => v.Gia)
                    .SumAsync();

                // Đếm số vé đã bán
                var soVe = await _context.CTHDs
                    .Join(_context.HoaDons, ct => ct.MaHoaDon, hd => hd.MaHoaDon, (ct, hd) => new { ct, hd })
                    .Where(x => x.hd.ThoiGianTao.Date == date)
                    .CountAsync();

                result.Add(new DoanhThuTheoNgayViewModel
                {
                    Ngay = date,
                    DoanhThu = doanhThu,
                    SoVe = soVe
                });
            }

            return result;
        }

        private async Task<List<DoanhThuTheoThangViewModel>> GetDoanhThuTheoThang(int months)
        {
            var result = new List<DoanhThuTheoThangViewModel>();
            var startDate = DateTime.Today.AddMonths(-months + 1);
            startDate = new DateTime(startDate.Year, startDate.Month, 1);

            for (int i = 0; i < months; i++)
            {
                var date = startDate.AddMonths(i);
                var nextMonth = date.AddMonths(1);

                var doanhThu = await _context.Ves
                    .Where(v => v.HanSuDung >= date && v.HanSuDung < nextMonth)
                    .SumAsync(v => v.Gia);

                var soVe = await _context.Ves
                    .CountAsync(v => v.HanSuDung >= date && v.HanSuDung < nextMonth);

                result.Add(new DoanhThuTheoThangViewModel
                {
                    Thang = date.Month,
                    Nam = date.Year,
                    DoanhThu = doanhThu,
                    SoVe = soVe
                });
            }

            return result;
        }
    }
}
