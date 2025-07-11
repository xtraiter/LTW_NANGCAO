using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CinemaManagement.Data;
using CinemaManagement.Models;
using CinemaManagement.ViewModels;

namespace CinemaManagement.Controllers
{
    public class QuanLyController : Controller
    {
        private readonly CinemaDbContext _context;

        public QuanLyController(CinemaDbContext context)
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

        public async Task<IActionResult> Index()
        {
            if (!IsManagerOrStaff())
            {
                return RedirectToAction("Login", "Auth");
            }

            var today = DateTime.Today;
            var thisWeek = today.AddDays(-(int)today.DayOfWeek);
            var thisMonth = new DateTime(today.Year, today.Month, 1);

            var dashboard = new DashboardViewModel
            {
                // Thống kê cơ bản
                TongSoVe = await _context.Ves.CountAsync(),
                VeHomNay = await _context.Ves.CountAsync(v => v.HanSuDung.Date == today),
                VeTuanNay = await _context.Ves.CountAsync(v => v.HanSuDung >= thisWeek),
                VeThangNay = await _context.Ves.CountAsync(v => v.HanSuDung >= thisMonth),

                // Thống kê doanh thu
                DoanhThuHomNay = await _context.Ves.Where(v => v.HanSuDung.Date == today).SumAsync(v => v.Gia),
                DoanhThuTuanNay = await _context.Ves.Where(v => v.HanSuDung >= thisWeek).SumAsync(v => v.Gia),
                DoanhThuThangNay = await _context.Ves.Where(v => v.HanSuDung >= thisMonth).SumAsync(v => v.Gia),

                // Thống kê lịch chiếu
                LichChieuHomNay = await _context.LichChieus.CountAsync(l => l.ThoiGianBatDau.Date == today),
                LichChieuTuanNay = await _context.LichChieus.CountAsync(l => l.ThoiGianBatDau >= thisWeek),

                // Thống kê phim
                TongSoPhim = await _context.Phims.CountAsync(),
                PhimDangChieu = await _context.LichChieus
                    .Where(l => l.ThoiGianBatDau >= DateTime.Now)
                    .Select(l => l.MaPhim)
                    .Distinct()
                    .CountAsync(),

                // Thống kê phòng chiếu
                TongSoPhong = await _context.PhongChieus.CountAsync(),
                TongSoGhe = await _context.GheNgois.CountAsync(),

                // Thống kê trạng thái vé
                VeConHan = await _context.Ves.CountAsync(v => v.TrangThai == "Còn hạn"),
                VeHetHan = await _context.Ves.CountAsync(v => v.TrangThai == "Hết hạn"),
                VeDaBan = await _context.Ves.CountAsync(v => v.TrangThai == "Đã sử dụng"),

                // Top phim bán chạy
                TopPhimBanChay = await _context.Ves
                    .Include(v => v.Phim)
                    .GroupBy(v => new { v.MaPhim, v.TenPhim })
                    .Select(g => new TopPhimViewModel
                    {
                        MaPhim = g.Key.MaPhim,
                        TenPhim = g.Key.TenPhim,
                        SoVe = g.Count(),
                        DoanhThu = g.Sum(v => v.Gia)
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

                // Doanh thu theo ngày (7 ngày gần nhất)
                DoanhThuTheoNgay = await GetDoanhThuTheoNgay(7),

                // Thống kê theo tháng (12 tháng gần nhất)
                DoanhThuTheoThang = await GetDoanhThuTheoThang(12)
            };

            return View(dashboard);
        }

        public async Task<IActionResult> ThongKeChiTiet()
        {
            if (!IsManagerOrStaff())
            {
                return RedirectToAction("Login", "Auth");
            }

            var thongKe = new ThongKeChiTietViewModel
            {
                // Thống kê tổng quan
                TongSoVe = await _context.Ves.CountAsync(),
                TongDoanhThu = await _context.Ves.SumAsync(v => v.Gia),
                TongSoPhim = await _context.Phims.CountAsync(),
                TongSoLichChieu = await _context.LichChieus.CountAsync(),

                // Thống kê theo phim
                ThongKeTheoPhim = await _context.Ves
                    .Include(v => v.Phim)
                    .GroupBy(v => new { v.MaPhim, v.TenPhim })
                    .Select(g => new ThongKePhimChiTietViewModel
                    {
                        MaPhim = g.Key.MaPhim,
                        TenPhim = g.Key.TenPhim,
                        SoVe = g.Count(),
                        DoanhThu = g.Sum(v => v.Gia),
                        GiaTrungBinh = g.Average(v => v.Gia)
                    })
                    .OrderByDescending(t => t.DoanhThu)
                    .ToListAsync(),

                // Thống kê theo phòng
                ThongKeTheoPhong = await _context.Ves
                    .Include(v => v.PhongChieu)
                    .GroupBy(v => new { v.MaPhong, v.TenPhong })
                    .Select(g => new ThongKePhongViewModel
                    {
                        MaPhong = g.Key.MaPhong,
                        TenPhong = g.Key.TenPhong,
                        SoVe = g.Count(),
                        DoanhThu = g.Sum(v => v.Gia),
                        TiLeLapDay = 0 // Sẽ tính toán sau
                    })
                    .OrderByDescending(t => t.DoanhThu)
                    .ToListAsync(),

                // Thống kê theo thời gian
                DoanhThuTheoNgay = await GetDoanhThuTheoNgay(30),
                DoanhThuTheoThang = await GetDoanhThuTheoThang(12)
            };

            return View(thongKe);
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

        public async Task<IActionResult> QuanLyPhim()
        {
            if (!IsManager())
            {
                return RedirectToAction("Login", "Auth");
            }

            var phims = await _context.Phims.ToListAsync();
            return View(phims);
        }

        public async Task<IActionResult> QuanLyLichChieu()
        {
            if (!IsManager())
            {
                return RedirectToAction("Login", "Auth");
            }

            var lichChieus = await _context.LichChieus
                .Include(l => l.Phim)
                .Include(l => l.PhongChieu)
                .Include(l => l.NhanVien)
                .OrderBy(l => l.ThoiGianBatDau)
                .ToListAsync();

            return View(lichChieus);
        }

        public async Task<IActionResult> QuanLyNhanVien()
        {
            if (!IsManager())
            {
                return RedirectToAction("Login", "Auth");
            }

            var nhanViens = await _context.NhanViens
                .OrderBy(n => n.TenNhanVien)
                .ToListAsync();

            return View(nhanViens);
        }

        public async Task<IActionResult> BaoCao()
        {
            if (!IsManager())
            {
                return RedirectToAction("Login", "Auth");
            }

            var baoCao = new BaoCaoViewModel
            {
                TongDoanhThu = await _context.Ves.SumAsync(v => v.Gia),
                TongSoVe = await _context.Ves.CountAsync(),
                TongSoPhim = await _context.Phims.CountAsync(),
                TongSoLichChieu = await _context.LichChieus.CountAsync(),
                DoanhThuTheoPhim = await _context.Ves
                    .GroupBy(v => v.TenPhim)
                    .Select(g => new { TenPhim = g.Key, DoanhThu = g.Sum(v => v.Gia) })
                    .OrderByDescending(x => x.DoanhThu)
                    .Take(10)
                    .ToDictionaryAsync(x => x.TenPhim, x => x.DoanhThu)
            };

            return View(baoCao);
        }

        [HttpPost]
        public async Task<IActionResult> ThemPhim(string tenPhim, string theLoai, int thoiLuong, string doTuoiPhanAnh, string moTa, string viTriFilePhim)
        {
            if (!IsManager())
            {
                return Json(new { success = false, message = "Không có quyền truy cập" });
            }

            try
            {
                // Tạo mã phim mới
                var lastPhim = await _context.Phims.OrderByDescending(p => p.MaPhim).FirstOrDefaultAsync();
                var newMaPhim = "P001";
                if (lastPhim != null)
                {
                    var lastNumber = int.Parse(lastPhim.MaPhim.Substring(1));
                    newMaPhim = $"P{(lastNumber + 1):D3}";
                }

                var maNhanVien = HttpContext.Session.GetString("MaNhanVien");
                if (string.IsNullOrEmpty(maNhanVien))
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin nhân viên" });
                }

                var phim = new Phim
                {
                    MaPhim = newMaPhim,
                    TenPhim = tenPhim,
                    TheLoai = theLoai,
                    ThoiLuong = thoiLuong,
                    DoTuoiPhanAnh = doTuoiPhanAnh,
                    MoTa = moTa,
                    ViTriFilePhim = viTriFilePhim,
                    MaNhanVien = maNhanVien
                };

                _context.Phims.Add(phim);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Thêm phim thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi thêm phim: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> XoaPhim(string maPhim)
        {
            if (!IsManager())
            {
                return Json(new { success = false, message = "Không có quyền truy cập" });
            }

            try
            {
                var phim = await _context.Phims.FirstOrDefaultAsync(p => p.MaPhim == maPhim);
                if (phim == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy phim" });
                }

                // Kiểm tra xem phim có được sử dụng trong lịch chiếu không
                var coLichChieu = await _context.LichChieus.AnyAsync(l => l.MaPhim == maPhim);
                if (coLichChieu)
                {
                    return Json(new { success = false, message = "Không thể xóa phim đang có lịch chiếu" });
                }

                _context.Phims.Remove(phim);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Xóa phim thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi xóa phim: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ChiTietPhim(string maPhim)
        {
            if (!IsManagerOrStaff())
            {
                return Json(new { success = false, message = "Không có quyền truy cập" });
            }

            var phim = await _context.Phims
                .Include(p => p.NhanVien)
                .FirstOrDefaultAsync(p => p.MaPhim == maPhim);

            if (phim == null)
            {
                return Json(new { success = false, message = "Không tìm thấy phim" });
            }

            var lichChieuCount = await _context.LichChieus.CountAsync(l => l.MaPhim == maPhim);
            var veCount = await _context.Ves.CountAsync(v => v.MaPhim == maPhim);

            return Json(new
            {
                success = true,
                phim = new
                {
                    MaPhim = phim.MaPhim,
                    TenPhim = phim.TenPhim,
                    TheLoai = phim.TheLoai,
                    ThoiLuong = phim.ThoiLuong,
                    DoTuoiPhanAnh = phim.DoTuoiPhanAnh,
                    MoTa = phim.MoTa,
                    ViTriFilePhim = phim.ViTriFilePhim,
                    NhanVien = phim.NhanVien.TenNhanVien,
                    SoLichChieu = lichChieuCount,
                    SoVeBanRa = veCount
                }
            });
        }
    }
}
