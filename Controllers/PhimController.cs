using Microsoft.AspNetCore.Mvc;
using CinemaManagement.Data;
using CinemaManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaManagement.Controllers
{
    public class PhimController : Controller
    {
        private readonly CinemaDbContext _context;

        public PhimController(CinemaDbContext context)
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

        public async Task<IActionResult> Index(string searchTerm, string theLoai, string doTuoi, string sortBy = "TenPhim")
        {
            if (!IsManager())
            {
                return RedirectToAction("Index", "Home");
            }

            var query = _context.Phims.Include(p => p.NhanVien).AsQueryable();

            // Áp dụng bộ lọc
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(p => p.TenPhim.Contains(searchTerm) || p.MaPhim.Contains(searchTerm));
            }

            if (!string.IsNullOrEmpty(theLoai))
            {
                query = query.Where(p => p.TheLoai == theLoai);
            }

            if (!string.IsNullOrEmpty(doTuoi))
            {
                query = query.Where(p => p.DoTuoiPhanAnh == doTuoi);
            }

            // Áp dụng sắp xếp
            query = sortBy switch
            {
                "TenPhim" => query.OrderBy(p => p.TenPhim),
                "TheLoai" => query.OrderBy(p => p.TheLoai),
                "ThoiLuong" => query.OrderByDescending(p => p.ThoiLuong),
                "MaPhim" => query.OrderBy(p => p.MaPhim),
                _ => query.OrderBy(p => p.TenPhim)
            };

            var phims = await query.ToListAsync();

            // Truyền dữ liệu cho bộ lọc
            ViewBag.SearchTerm = searchTerm;
            ViewBag.TheLoai = theLoai;
            ViewBag.DoTuoi = doTuoi;
            ViewBag.SortBy = sortBy;
            
            // Danh sách thể loại và độ tuổi để hiển thị trong dropdown
            ViewBag.DanhSachTheLoai = await _context.Phims.Select(p => p.TheLoai).Distinct().ToListAsync();
            ViewBag.DanhSachDoTuoi = await _context.Phims.Select(p => p.DoTuoiPhanAnh).Distinct().ToListAsync();

            return View(phims);
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
                // Tạo mã phim mới theo định dạng PH001
                var lastPhim = await _context.Phims.OrderByDescending(p => p.MaPhim).FirstOrDefaultAsync();
                var newMaPhim = "PH001";
                if (lastPhim != null)
                {
                    // Lấy số từ mã phim cuối (ví dụ: PH001 -> 001)
                    var lastNumberStr = lastPhim.MaPhim.Substring(2);
                    if (int.TryParse(lastNumberStr, out int lastNumber))
                    {
                        newMaPhim = $"PH{(lastNumber + 1):D3}";
                    }
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
                    ViTriFilePhim = viTriFilePhim ?? string.Empty,
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
        public async Task<IActionResult> SuaPhim(string maPhim, string tenPhim, string theLoai, int thoiLuong, string doTuoiPhanAnh, string moTa, string viTriFilePhim)
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

                // Cập nhật thông tin phim
                phim.TenPhim = tenPhim;
                phim.TheLoai = theLoai;
                phim.ThoiLuong = thoiLuong;
                phim.DoTuoiPhanAnh = doTuoiPhanAnh;
                phim.MoTa = moTa;
                phim.ViTriFilePhim = viTriFilePhim ?? phim.ViTriFilePhim;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Cập nhật phim thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi cập nhật phim: " + ex.Message });
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
        public async Task<IActionResult> ChiTiet(string id)
        {
            if (!IsManagerOrStaff())
            {
                return RedirectToAction("Index", "Home");
            }

            var phim = await _context.Phims
                .Include(p => p.NhanVien)
                .FirstOrDefaultAsync(p => p.MaPhim == id);

            if (phim == null)
            {
                TempData["Error"] = "Không tìm thấy phim với mã " + id;
                return RedirectToAction("Index");
            }

            // Thống kê thêm
            var lichChieuCount = await _context.LichChieus.CountAsync(l => l.MaPhim == id);
            
            // Đếm số vé đã bán thông qua hóa đơn
            var veBanRaCount = await _context.CTHDs
                .Include(c => c.Ve)
                .Where(c => c.Ve.MaPhim == id)
                .CountAsync();
            
            // Tính doanh thu từ vé đã bán trong hóa đơn
            var doanhThu = await _context.CTHDs
                .Include(c => c.Ve)
                .Where(c => c.Ve.MaPhim == id)
                .SumAsync(c => (decimal?)c.DonGia) ?? 0;

            ViewBag.SoLichChieu = lichChieuCount;
            ViewBag.SoVeBanRa = veBanRaCount;
            ViewBag.DoanhThu = doanhThu;
            ViewBag.IsManager = IsManager();

            return View(phim);
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
            
            // Đếm số vé đã bán thông qua hóa đơn
            var veBanRaCount = await _context.CTHDs
                .Include(c => c.Ve)
                .Where(c => c.Ve.MaPhim == maPhim)
                .CountAsync();
            
            // Tính doanh thu từ vé đã bán trong hóa đơn
            var doanhThu = await _context.CTHDs
                .Include(c => c.Ve)
                .Where(c => c.Ve.MaPhim == maPhim)
                .SumAsync(c => (decimal?)c.DonGia) ?? 0;

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
                    NhanVien = phim.NhanVien?.TenNhanVien ?? "Chưa có thông tin",
                    SoLichChieu = lichChieuCount,
                    SoVeBanRa = veBanRaCount,
                    DoanhThu = doanhThu
                }
            });
        }

        [HttpGet]
        public async Task<IActionResult> SearchSuggestions(string term)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            {
                return Json(new { suggestions = new object[0] });
            }

            var suggestions = await _context.Phims
                .Where(p => p.TenPhim.Contains(term) || p.TheLoai.Contains(term))
                .Select(p => new
                {
                    MaPhim = p.MaPhim,
                    TenPhim = p.TenPhim,
                    TheLoai = p.TheLoai,
                    ThoiLuong = p.ThoiLuong,
                    DoTuoiPhanAnh = p.DoTuoiPhanAnh,
                    ViTriFilePhim = p.ViTriFilePhim,
                    MoTa = p.MoTa.Length > 100 ? p.MoTa.Substring(0, 100) + "..." : p.MoTa
                })
                .Take(8)
                .ToListAsync();

            return Json(new { suggestions });
        }

        [HttpGet]
        public async Task<IActionResult> SearchMovies(string searchTerm, string theLoai, int page = 1, int pageSize = 12)
        {
            var query = _context.Phims.AsQueryable();

            // Áp dụng bộ lọc tìm kiếm
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(p => p.TenPhim.Contains(searchTerm) || 
                                       p.TheLoai.Contains(searchTerm) ||
                                       p.MoTa.Contains(searchTerm));
            }

            if (!string.IsNullOrWhiteSpace(theLoai) && theLoai != "all")
            {
                query = query.Where(p => p.TheLoai == theLoai);
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var movies = await query
                .OrderBy(p => p.TenPhim)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    MaPhim = p.MaPhim,
                    TenPhim = p.TenPhim,
                    TheLoai = p.TheLoai,
                    ThoiLuong = p.ThoiLuong,
                    DoTuoiPhanAnh = p.DoTuoiPhanAnh,
                    ViTriFilePhim = p.ViTriFilePhim,
                    MoTa = p.MoTa.Length > 150 ? p.MoTa.Substring(0, 150) + "..." : p.MoTa
                })
                .ToListAsync();

            return Json(new
            {
                movies,
                totalCount,
                totalPages,
                currentPage = page,
                hasNextPage = page < totalPages,
                hasPreviousPage = page > 1
            });
        }

        [HttpGet]
        public async Task<IActionResult> TimKiem()
        {
            // Lấy danh sách thể loại để hiển thị filter
            var theLoaiList = await _context.Phims
                .Select(p => p.TheLoai)
                .Distinct()
                .OrderBy(t => t)
                .ToListAsync();

            ViewBag.TheLoaiList = theLoaiList;
            
            return View();
        }
    }
}
