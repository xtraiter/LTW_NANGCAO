using Microsoft.AspNetCore.Mvc;
using CinemaManagement.Data;
using CinemaManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaManagement.Controllers
{
    public class LichChieuController : Controller
    {
        private readonly CinemaDbContext _context;

        public LichChieuController(CinemaDbContext context)
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

        public async Task<IActionResult> Index(string searchTerm, string maPhim, string maPhong, DateTime? ngayChieu, string trangThai, string sortBy = "ThoiGianBatDau")
        {
            if (!IsManager())
            {
                return RedirectToAction("Index", "Home");
            }

            var query = _context.LichChieus
                .Include(l => l.Phim)
                .Include(l => l.PhongChieu)
                .Include(l => l.NhanVien)
                .AsQueryable();

            // Áp dụng bộ lọc
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(l => l.MaLichChieu.Contains(searchTerm) || 
                                       l.Phim.TenPhim.Contains(searchTerm) ||
                                       l.PhongChieu.TenPhong.Contains(searchTerm));
            }

            if (!string.IsNullOrEmpty(maPhim))
            {
                query = query.Where(l => l.MaPhim == maPhim);
            }

            if (!string.IsNullOrEmpty(maPhong))
            {
                query = query.Where(l => l.MaPhong == maPhong);
            }

            if (ngayChieu.HasValue)
            {
                var ngay = ngayChieu.Value.Date;
                query = query.Where(l => l.ThoiGianBatDau.Date == ngay);
            }

            if (!string.IsNullOrEmpty(trangThai))
            {
                var now = DateTime.Now;
                if (trangThai == "SapChieu")
                {
                    query = query.Where(l => l.ThoiGianBatDau > now);
                }
                else if (trangThai == "DangChieu")
                {
                    query = query.Where(l => l.ThoiGianBatDau <= now && l.ThoiGianKetThuc > now);
                }
                else if (trangThai == "KetThuc")
                {
                    query = query.Where(l => l.ThoiGianKetThuc <= now);
                }
            }

            // Áp dụng sắp xếp
            query = sortBy switch
            {
                "ThoiGianBatDau" => query.OrderBy(l => l.ThoiGianBatDau),
                "ThoiGianBatDau_desc" => query.OrderByDescending(l => l.ThoiGianBatDau),
                "TenPhim" => query.OrderBy(l => l.Phim.TenPhim),
                "TenPhong" => query.OrderBy(l => l.PhongChieu.TenPhong),
                "Gia" => query.OrderByDescending(l => l.Gia),
                "MaLichChieu" => query.OrderBy(l => l.MaLichChieu),
                _ => query.OrderBy(l => l.ThoiGianBatDau)
            };

            var lichChieus = await query.ToListAsync();

            // Truyền dữ liệu cho bộ lọc
            ViewBag.SearchTerm = searchTerm;
            ViewBag.MaPhim = maPhim;
            ViewBag.MaPhong = maPhong;
            ViewBag.NgayChieu = ngayChieu?.ToString("yyyy-MM-dd");
            ViewBag.TrangThai = trangThai;
            ViewBag.SortBy = sortBy;
            
            // Danh sách để hiển thị trong dropdown
            ViewBag.DanhSachPhim = await _context.Phims
                .Select(p => new { p.MaPhim, p.TenPhim })
                .ToListAsync();
            ViewBag.DanhSachPhong = await _context.PhongChieus
                .Where(p => p.TrangThai == "Hoạt động")
                .Select(p => new { p.MaPhong, p.TenPhong, p.LoaiPhong })
                .ToListAsync();

            return View(lichChieus);
        }

        [HttpPost]
        public async Task<IActionResult> ThemLichChieu(string maPhim, string maPhong, DateTime thoiGianBatDau, decimal gia)
        {
            if (!IsManager())
            {
                return Json(new { success = false, message = "Không có quyền truy cập" });
            }

            try
            {
                // Kiểm tra phim có tồn tại
                var phim = await _context.Phims.FirstOrDefaultAsync(p => p.MaPhim == maPhim);
                if (phim == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy phim" });
                }

                // Kiểm tra phòng có tồn tại và hoạt động
                var phong = await _context.PhongChieus.FirstOrDefaultAsync(p => p.MaPhong == maPhong);
                if (phong == null || phong.TrangThai != "Hoạt động")
                {
                    return Json(new { success = false, message = "Phòng chiếu không khả dụng" });
                }

                // Tính thời gian kết thúc (thêm 15 phút cho quảng cáo và dọn dẹp)
                var thoiGianKetThuc = thoiGianBatDau.AddMinutes(phim.ThoiLuong + 15);

                // Kiểm tra xung đột lịch chiếu
                var conflictSchedule = await _context.LichChieus
                    .Where(l => l.MaPhong == maPhong &&
                               ((l.ThoiGianBatDau <= thoiGianBatDau && l.ThoiGianKetThuc > thoiGianBatDau) ||
                                (l.ThoiGianBatDau < thoiGianKetThuc && l.ThoiGianKetThuc >= thoiGianKetThuc) ||
                                (l.ThoiGianBatDau >= thoiGianBatDau && l.ThoiGianKetThuc <= thoiGianKetThuc)))
                    .FirstOrDefaultAsync();

                if (conflictSchedule != null)
                {
                    return Json(new { success = false, message = "Thời gian chiếu bị trung với lịch chiếu khác trong cùng phòng" });
                }

                // Tạo mã lịch chiếu mới
                var lastLichChieu = await _context.LichChieus.OrderByDescending(l => l.MaLichChieu).FirstOrDefaultAsync();
                var newMaLichChieu = "LC001";
                if (lastLichChieu != null)
                {
                    var lastNumberStr = lastLichChieu.MaLichChieu.Substring(2);
                    if (int.TryParse(lastNumberStr, out int lastNumber))
                    {
                        newMaLichChieu = $"LC{(lastNumber + 1):D3}";
                    }
                }

                var maNhanVien = HttpContext.Session.GetString("MaNhanVien");
                if (string.IsNullOrEmpty(maNhanVien))
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin nhân viên" });
                }

                var lichChieu = new LichChieu
                {
                    MaLichChieu = newMaLichChieu,
                    ThoiGianBatDau = thoiGianBatDau,
                    ThoiGianKetThuc = thoiGianKetThuc,
                    Gia = gia,
                    MaPhong = maPhong,
                    MaPhim = maPhim,
                    MaNhanVien = maNhanVien
                };

                _context.LichChieus.Add(lichChieu);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Thêm lịch chiếu thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi thêm lịch chiếu: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SuaLichChieu(string maLichChieu, string maPhim, string maPhong, DateTime thoiGianBatDau, decimal gia)
        {
            if (!IsManager())
            {
                return Json(new { success = false, message = "Không có quyền truy cập" });
            }

            try
            {
                var lichChieu = await _context.LichChieus.FirstOrDefaultAsync(l => l.MaLichChieu == maLichChieu);
                if (lichChieu == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy lịch chiếu" });
                }

                // Kiểm tra đã có vé được bán chưa
                var veDaBan = await _context.CTHDs.Include(c => c.Ve).AnyAsync(c => c.Ve.MaLichChieu == maLichChieu);
                if (veDaBan)
                {
                    return Json(new { success = false, message = "Không thể sửa lịch chiếu đã có vé được bán" });
                }

                // Kiểm tra phim có tồn tại
                var phim = await _context.Phims.FirstOrDefaultAsync(p => p.MaPhim == maPhim);
                if (phim == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy phim" });
                }

                // Kiểm tra phòng có tồn tại và hoạt động
                var phong = await _context.PhongChieus.FirstOrDefaultAsync(p => p.MaPhong == maPhong);
                if (phong == null || phong.TrangThai != "Hoạt động")
                {
                    return Json(new { success = false, message = "Phòng chiếu không khả dụng" });
                }

                // Tính thời gian kết thúc
                var thoiGianKetThuc = thoiGianBatDau.AddMinutes(phim.ThoiLuong + 15);

                // Kiểm tra xung đột lịch chiếu (loại trừ lịch chiếu hiện tại)
                var conflictSchedule = await _context.LichChieus
                    .Where(l => l.MaPhong == maPhong && l.MaLichChieu != maLichChieu &&
                               ((l.ThoiGianBatDau <= thoiGianBatDau && l.ThoiGianKetThuc > thoiGianBatDau) ||
                                (l.ThoiGianBatDau < thoiGianKetThuc && l.ThoiGianKetThuc >= thoiGianKetThuc) ||
                                (l.ThoiGianBatDau >= thoiGianBatDau && l.ThoiGianKetThuc <= thoiGianKetThuc)))
                    .FirstOrDefaultAsync();

                if (conflictSchedule != null)
                {
                    return Json(new { success = false, message = "Thời gian chiếu bị trung với lịch chiếu khác trong cùng phòng" });
                }

                // Cập nhật thông tin
                lichChieu.MaPhim = maPhim;
                lichChieu.MaPhong = maPhong;
                lichChieu.ThoiGianBatDau = thoiGianBatDau;
                lichChieu.ThoiGianKetThuc = thoiGianKetThuc;
                lichChieu.Gia = gia;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Cập nhật lịch chiếu thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi cập nhật lịch chiếu: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> XoaLichChieu(string maLichChieu)
        {
            if (!IsManager())
            {
                return Json(new { success = false, message = "Không có quyền truy cập" });
            }

            try
            {
                var lichChieu = await _context.LichChieus.FirstOrDefaultAsync(l => l.MaLichChieu == maLichChieu);
                if (lichChieu == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy lịch chiếu" });
                }

                // Kiểm tra đã có vé được bán chưa
                var veDaBan = await _context.CTHDs.Include(c => c.Ve).AnyAsync(c => c.Ve.MaLichChieu == maLichChieu);
                if (veDaBan)
                {
                    return Json(new { success = false, message = "Không thể xóa lịch chiếu đã có vé được bán" });
                }

                _context.LichChieus.Remove(lichChieu);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Xóa lịch chiếu thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi xóa lịch chiếu: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ChiTietLichChieu(string maLichChieu)
        {
            if (!IsManagerOrStaff())
            {
                return Json(new { success = false, message = "Không có quyền truy cập" });
            }

            var lichChieu = await _context.LichChieus
                .Include(l => l.Phim)
                .Include(l => l.PhongChieu)
                .Include(l => l.NhanVien)
                .FirstOrDefaultAsync(l => l.MaLichChieu == maLichChieu);

            if (lichChieu == null)
            {
                return Json(new { success = false, message = "Không tìm thấy lịch chiếu" });
            }

            // Thống kê vé
            var tongGhe = await _context.GheNgois.CountAsync(g => g.MaPhong == lichChieu.MaPhong);
            var veDaBan = await _context.CTHDs.Include(c => c.Ve).CountAsync(c => c.Ve.MaLichChieu == maLichChieu);
            var doanhThu = await _context.CTHDs.Include(c => c.Ve).Where(c => c.Ve.MaLichChieu == maLichChieu).SumAsync(c => (decimal?)c.DonGia) ?? 0;

            var now = DateTime.Now;
            string trangThai;
            if (lichChieu.ThoiGianBatDau > now)
                trangThai = "Sắp chiếu";
            else if (lichChieu.ThoiGianKetThuc > now)
                trangThai = "Đang chiếu";
            else
                trangThai = "Kết thúc";

            return Json(new
            {
                success = true,
                lichChieu = new
                {
                    MaLichChieu = lichChieu.MaLichChieu,
                    ThoiGianBatDau = lichChieu.ThoiGianBatDau.ToString("dd/MM/yyyy HH:mm"),
                    ThoiGianKetThuc = lichChieu.ThoiGianKetThuc.ToString("dd/MM/yyyy HH:mm"),
                    Gia = lichChieu.Gia,
                    TenPhim = lichChieu.Phim.TenPhim,
                    MaPhim = lichChieu.MaPhim,
                    TenPhong = lichChieu.PhongChieu.TenPhong,
                    MaPhong = lichChieu.MaPhong,
                    LoaiPhong = lichChieu.PhongChieu.LoaiPhong,
                    TenNhanVien = lichChieu.NhanVien?.TenNhanVien ?? "Chưa có thông tin",
                    TongGhe = tongGhe,
                    VeDaBan = veDaBan,
                    GheConLai = tongGhe - veDaBan,
                    DoanhThu = doanhThu,
                    TrangThai = trangThai,
                    ThoiLuongPhim = lichChieu.Phim.ThoiLuong
                }
            });
        }
    }
}
