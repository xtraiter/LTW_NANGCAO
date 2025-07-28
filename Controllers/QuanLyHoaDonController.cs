using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CinemaManagement.Data;
using CinemaManagement.Models;
using CinemaManagement.ViewModels;

namespace CinemaManagement.Controllers
{
    public class QuanLyHoaDonController : Controller
    {
        private readonly CinemaDbContext _context;
        private readonly ILogger<QuanLyHoaDonController> _logger;

        public QuanLyHoaDonController(CinemaDbContext context, ILogger<QuanLyHoaDonController> logger)
        {
            _context = context;
            _logger = logger;
        }

        private bool IsAdminLoggedIn()
        {
            return !string.IsNullOrEmpty(HttpContext.Session.GetString("MaNhanVien"));
        }

        // GET: QuanLyHoaDon
        public async Task<IActionResult> Index(
            string? trangThaiFilter,
            DateTime? tuNgay,
            DateTime? denNgay,
            string? searchTerm,
            int page = 1,
            int pageSize = 10)
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login", "Auth");

            var query = _context.HoaDonSanPhams
                .Include(h => h.KhachHang)
                .Include(h => h.ChiTietHoaDonSanPhams)
                    .ThenInclude(c => c.SanPham)
                .AsQueryable();

            // Lọc theo trạng thái
            if (!string.IsNullOrEmpty(trangThaiFilter))
            {
                query = query.Where(h => h.TrangThai == trangThaiFilter);
            }

            // Lọc theo ngày
            if (tuNgay.HasValue)
            {
                query = query.Where(h => h.ThoiGianTao >= tuNgay.Value);
            }
            if (denNgay.HasValue)
            {
                query = query.Where(h => h.ThoiGianTao <= denNgay.Value.AddDays(1));
            }

            // Tìm kiếm
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(h => 
                    h.MaHoaDonSanPham.Contains(searchTerm) ||
                    h.KhachHang.HoTen.Contains(searchTerm) ||
                    h.KhachHang.SDT.Contains(searchTerm) ||
                    h.DiaChiGiaoHang.Contains(searchTerm));
            }

            // Sắp xếp theo thời gian tạo mới nhất
            query = query.OrderByDescending(h => h.ThoiGianTao);

            // Phân trang
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            var hoaDons = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Thống kê
            var thongKe = new
            {
                TongDonHang = await _context.HoaDonSanPhams.CountAsync(),
                DonHangMoi = await _context.HoaDonSanPhams.CountAsync(h => h.TrangThai == "Đã đặt"),
                DonHangDangXuLy = await _context.HoaDonSanPhams.CountAsync(h => h.TrangThai == "Đang xử lý"),
                DonHangDaGiao = await _context.HoaDonSanPhams.CountAsync(h => h.TrangThai == "Đã giao"),
                DonHangDaHuy = await _context.HoaDonSanPhams.CountAsync(h => h.TrangThai == "Đã hủy"),
                TongDoanhThu = await _context.HoaDonSanPhams
                    .Where(h => h.TrangThai == "Đã giao")
                    .SumAsync(h => h.TongTien)
            };

            ViewBag.TrangThaiFilter = trangThaiFilter;
            ViewBag.TuNgay = tuNgay;
            ViewBag.DenNgay = denNgay;
            ViewBag.SearchTerm = searchTerm;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.ThongKe = thongKe;

            return View(hoaDons);
        }

        // GET: QuanLyHoaDon/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login", "Auth");

            if (string.IsNullOrEmpty(id))
                return NotFound();

            var hoaDon = await _context.HoaDonSanPhams
                .Include(h => h.KhachHang)
                .Include(h => h.ChiTietHoaDonSanPhams)
                    .ThenInclude(c => c.SanPham)
                .Include(h => h.HoaDonSanPhamVouchers)
                    .ThenInclude(hv => hv.VoucherSanPham)
                .FirstOrDefaultAsync(h => h.MaHoaDonSanPham == id);

            if (hoaDon == null)
                return NotFound();

            return View(hoaDon);
        }

        // POST: QuanLyHoaDon/CapNhatTrangThai
        [HttpPost]
        public async Task<IActionResult> CapNhatTrangThai(string maHoaDon, string trangThaiMoi, string? ghiChu = null)
        {
            if (!IsAdminLoggedIn())
                return Json(new { success = false, message = "Chưa đăng nhập" });

            try
            {
                var hoaDon = await _context.HoaDonSanPhams
                    .FirstOrDefaultAsync(h => h.MaHoaDonSanPham == maHoaDon);

            if (hoaDon == null)
                    return Json(new { success = false, message = "Không tìm thấy hóa đơn" });

                // Kiểm tra quyền chuyển trạng thái
                if (!KiemTraQuyenChuyenTrangThai(hoaDon.TrangThai, trangThaiMoi))
                {
                    return Json(new { success = false, message = "Không thể chuyển từ trạng thái này sang trạng thái khác" });
        }

                hoaDon.TrangThai = trangThaiMoi;
                await _context.SaveChangesAsync();

                // Ghi log thay đổi trạng thái
                _logger.LogInformation("Admin {MaNhanVien} đã cập nhật hóa đơn {MaHoaDon} từ {TrangThaiCu} sang {TrangThaiMoi}", 
                    HttpContext.Session.GetString("MaNhanVien"), maHoaDon, hoaDon.TrangThai, trangThaiMoi);

                return Json(new { success = true, message = "Cập nhật trạng thái thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status");
                return Json(new { success = false, message = "Có lỗi xảy ra khi cập nhật trạng thái" });
            }
        }

        // POST: QuanLyHoaDon/XoaHoaDon
        [HttpPost]
        public async Task<IActionResult> XoaHoaDon(string maHoaDon)
        {
            if (!IsAdminLoggedIn())
                return Json(new { success = false, message = "Chưa đăng nhập" });

            try
            {
                var hoaDon = await _context.HoaDonSanPhams
                    .Include(h => h.ChiTietHoaDonSanPhams)
                    .Include(h => h.HoaDonSanPhamVouchers)
                    .FirstOrDefaultAsync(h => h.MaHoaDonSanPham == maHoaDon);

                if (hoaDon == null)
                    return Json(new { success = false, message = "Không tìm thấy hóa đơn" });

                // Chỉ cho phép xóa hóa đơn đã hủy hoặc chưa xử lý
                if (hoaDon.TrangThai != "Đã hủy" && hoaDon.TrangThai != "Đã đặt")
                {
                    return Json(new { success = false, message = "Chỉ có thể xóa hóa đơn đã hủy hoặc chưa xử lý" });
                }

                // Hoàn trả tồn kho nếu cần
                foreach (var chiTiet in hoaDon.ChiTietHoaDonSanPhams)
                {
                    var sanPham = await _context.SanPhams.FindAsync(chiTiet.MaSanPham);
                    if (sanPham != null)
                    {
                        sanPham.SoLuongTon += chiTiet.SoLuong;
                    }
                }

                _context.ChiTietHoaDonSanPhams.RemoveRange(hoaDon.ChiTietHoaDonSanPhams);
                _context.HoaDonSanPhamVouchers.RemoveRange(hoaDon.HoaDonSanPhamVouchers);
                _context.HoaDonSanPhams.Remove(hoaDon);
                    
                    await _context.SaveChangesAsync();
                    
                return Json(new { success = true, message = "Xóa hóa đơn thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order");
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa hóa đơn" });
            }
        }

        // GET: QuanLyHoaDon/ThongKe
        public async Task<IActionResult> ThongKe()
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login", "Auth");

            var today = DateTime.Today;
            var thisMonth = new DateTime(today.Year, today.Month, 1);
            var lastMonth = thisMonth.AddMonths(-1);

            var thongKe = new
            {
                // Thống kê theo thời gian
                HomNay = await _context.HoaDonSanPhams
                    .Where(h => h.ThoiGianTao.Date == today)
                    .CountAsync(),
                ThangNay = await _context.HoaDonSanPhams
                    .Where(h => h.ThoiGianTao >= thisMonth)
                    .CountAsync(),
                ThangTruoc = await _context.HoaDonSanPhams
                    .Where(h => h.ThoiGianTao >= lastMonth && h.ThoiGianTao < thisMonth)
                    .CountAsync(),

                // Doanh thu
                DoanhThuHomNay = await _context.HoaDonSanPhams
                    .Where(h => h.ThoiGianTao.Date == today && h.TrangThai == "Đã giao")
                    .SumAsync(h => h.TongTien),
                DoanhThuThangNay = await _context.HoaDonSanPhams
                    .Where(h => h.ThoiGianTao >= thisMonth && h.TrangThai == "Đã giao")
                    .SumAsync(h => h.TongTien),

                // Trạng thái đơn hàng
                DangXuLy = await _context.HoaDonSanPhams.CountAsync(h => h.TrangThai == "Đang xử lý"),
                DaGiao = await _context.HoaDonSanPhams.CountAsync(h => h.TrangThai == "Đã giao"),
                DaHuy = await _context.HoaDonSanPhams.CountAsync(h => h.TrangThai == "Đã hủy"),

                // Top sản phẩm bán chạy
                TopSanPham = await _context.ChiTietHoaDonSanPhams
                    .Include(c => c.SanPham)
                    .GroupBy(c => c.MaSanPham)
                    .Select(g => new
                    {
                        MaSanPham = g.Key,
                        TenSanPham = g.First().SanPham.TenSanPham,
                        SoLuongBan = g.Sum(c => c.SoLuong),
                        DoanhThu = g.Sum(c => c.SoLuong * c.DonGia)
                    })
                    .OrderByDescending(x => x.SoLuongBan)
                    .Take(10)
                    .ToListAsync(),
                TongSoLuongBan = await _context.ChiTietHoaDonSanPhams.SumAsync(c => c.SoLuong),
                TongDoanhThuSanPham = await _context.ChiTietHoaDonSanPhams.SumAsync(c => c.SoLuong * c.DonGia)
            };

            return View(thongKe);
        }

        private bool KiemTraQuyenChuyenTrangThai(string trangThaiHienTai, string trangThaiMoi)
        {
            var quyTacChuyenTrangThai = new Dictionary<string, string[]>
            {
                { "Đã đặt", new[] { "Đang xử lý", "Đã hủy" } },
                { "Đang xử lý", new[] { "Đang giao", "Đã hủy" } },
                { "Đang giao", new[] { "Đã giao", "Đã hủy" } },
                { "Đã giao", new[] { "Đã hoàn thành" } },
                { "Đã hủy", new string[] { } } // Không thể chuyển từ đã hủy
            };

            return quyTacChuyenTrangThai.ContainsKey(trangThaiHienTai) &&
                   quyTacChuyenTrangThai[trangThaiHienTai].Contains(trangThaiMoi);
        }
    }
}
