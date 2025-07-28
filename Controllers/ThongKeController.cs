using Microsoft.AspNetCore.Mvc;
using CinemaManagement.Data;
using CinemaManagement.Models;
using CinemaManagement.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace CinemaManagement.Controllers
{
    public class ThongKeController : Controller
    {
        private readonly CinemaDbContext _context;

        public ThongKeController(CinemaDbContext context)
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

        public async Task<IActionResult> ChiTiet(DateTime? tuNgay, DateTime? denNgay, string? tenPhim)
        {
            if (!IsManagerOrStaff())
                return RedirectToAction("Index", "Home");

            // Query từ hóa đơn đã bán
            var cthdQuery = _context.CTHDs
                .Include(c => c.Ve)
                    .ThenInclude(v => v.Phim)
                .Include(c => c.Ve)
                    .ThenInclude(v => v.PhongChieu)
                .Include(c => c.HoaDon)
                .AsQueryable();

            if (tuNgay.HasValue)
                cthdQuery = cthdQuery.Where(c => c.HoaDon.ThoiGianTao.Date >= tuNgay.Value.Date);
            if (denNgay.HasValue)
                cthdQuery = cthdQuery.Where(c => c.HoaDon.ThoiGianTao.Date <= denNgay.Value.Date);
            if (!string.IsNullOrEmpty(tenPhim))
                cthdQuery = cthdQuery.Where(c => c.Ve.TenPhim.Contains(tenPhim));

            var thongKe = new ThongKeChiTietViewModel
            {
                TuNgay = tuNgay,
                DenNgay = denNgay,
                TenPhim = tenPhim,

                TongSoVe = await cthdQuery.CountAsync(),
                TongDoanhThu = await cthdQuery.SumAsync(c => c.DonGia),
                TongSoPhim = await _context.Phims.CountAsync(),
                TongSoLichChieu = await _context.LichChieus.CountAsync(),

                ThongKeTheoPhim = await cthdQuery
                    .GroupBy(c => new { c.Ve.MaPhim, c.Ve.TenPhim })
                    .Select(g => new ThongKePhimChiTietViewModel
                    {
                        MaPhim = g.Key.MaPhim,
                        TenPhim = g.Key.TenPhim,
                        SoVe = g.Count(),
                        DoanhThu = g.Sum(c => c.DonGia),
                        GiaTrungBinh = g.Average(c => c.DonGia)
                    })
                    .OrderByDescending(t => t.DoanhThu)
                    .ToListAsync(),

                ThongKeTheoPhong = await cthdQuery
                    .GroupBy(c => new { c.Ve.MaPhong, c.Ve.TenPhong })
                    .Select(g => new ThongKePhongViewModel
                    {
                        MaPhong = g.Key.MaPhong,
                        TenPhong = g.Key.TenPhong,
                        SoVe = g.Count(),
                        DoanhThu = g.Sum(c => c.DonGia),
                        TiLeLapDay = 0 // Có thể tính sau
                    })
                    .OrderByDescending(t => t.DoanhThu)
                    .ToListAsync(),

                DoanhThuTheoNgay = await GetDoanhThuHoaDonTheoNgay(30),
                DoanhThuTheoThang = await GetDoanhThuTheoThang(12)
            };

            return View(thongKe);
        }

        public async Task<IActionResult> BaoCao()
        {
            if (!IsManager())
            {
                return RedirectToAction("Index", "Home");
            }

            var baoCao = new BaoCaoViewModel
            {
                TongDoanhThu = await _context.CTHDs
                    .Join(_context.HoaDons, ct => ct.MaHoaDon, hd => hd.MaHoaDon, (ct, hd) => new { ct, hd })
                    .Join(_context.Ves, x => x.ct.MaVe, v => v.MaVe, (x, v) => v.Gia)
                    .SumAsync(),
                TongSoVe = await _context.CTHDs.CountAsync(),
                TongSoPhim = await _context.Phims.CountAsync(),
                TongSoLichChieu = await _context.LichChieus.CountAsync(),
                DoanhThuTheoPhim = await _context.CTHDs
                    .Join(_context.HoaDons, ct => ct.MaHoaDon, hd => hd.MaHoaDon, (ct, hd) => new { ct, hd })
                    .Join(_context.Ves, x => x.ct.MaVe, v => v.MaVe, (x, v) => v)
                    .GroupBy(v => v.TenPhim)
                    .Select(g => new { TenPhim = g.Key, DoanhThu = g.Sum(v => v.Gia) })
                    .OrderByDescending(x => x.DoanhThu)
                    .Take(10)
                    .ToDictionaryAsync(x => x.TenPhim, x => x.DoanhThu)
            };

            return View(baoCao);
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
