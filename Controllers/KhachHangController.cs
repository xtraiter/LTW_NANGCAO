using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CinemaManagement.Data;
using CinemaManagement.Models;
using CinemaManagement.ViewModels;
using CinemaManagement.Extensions;

namespace CinemaManagement.Controllers
{
    public class KhachHangController : Controller
    {
        private readonly CinemaDbContext _context;

        public KhachHangController(CinemaDbContext context)
        {
            _context = context;
        }

        // Middleware kiểm tra đăng nhập
        private bool IsCustomerLoggedIn()
        {
            var role = HttpContext.Session.GetString("Role");
            var maKhachHang = HttpContext.Session.GetString("MaKhachHang");
            return role == "Khách hàng" && !string.IsNullOrEmpty(maKhachHang);
        }

        // Trang chủ khách hàng - Hiển thị danh sách phim
        public async Task<IActionResult> Index(string? theLoai, string? searchTerm)
        {
            if (!IsCustomerLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            var phims = _context.Phims.AsQueryable();

            // Lọc theo thể loại
            if (!string.IsNullOrEmpty(theLoai))
            {
                phims = phims.Where(p => p.TheLoai.Contains(theLoai));
            }

            // Tìm kiếm theo tên phim
            if (!string.IsNullOrEmpty(searchTerm))
            {
                phims = phims.Where(p => p.TenPhim.Contains(searchTerm));
            }

            var phimList = await phims.ToListAsync();

            // Lấy danh sách thể loại để hiển thị filter
            ViewBag.TheLoais = await _context.Phims
                .Select(p => p.TheLoai)
                .Distinct()
                .ToListAsync();

            ViewBag.CurrentTheLoai = theLoai;
            ViewBag.CurrentSearch = searchTerm;

            return View(phimList);
        }

        // Chi tiết phim
        public async Task<IActionResult> ChiTietPhim(string id)
        {
            if (!IsCustomerLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var phim = await _context.Phims
                .Include(p => p.LichChieus)
                    .ThenInclude(lc => lc.PhongChieu)
                .FirstOrDefaultAsync(p => p.MaPhim == id);

            if (phim == null)
            {
                return NotFound();
            }

            // Lấy lịch chiếu từ hôm nay trở đi
            var lichChieuTuHienTai = phim.LichChieus
                .Where(lc => lc.ThoiGianBatDau >= DateTime.Now)
                .OrderBy(lc => lc.ThoiGianBatDau)
                .ToList();

            ViewBag.LichChieus = lichChieuTuHienTai;

            return View(phim);
        }

        // Chọn ghế ngồi
        public async Task<IActionResult> ChonGhe(string maLichChieu)
        {
            if (!IsCustomerLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            if (string.IsNullOrEmpty(maLichChieu))
            {
                return NotFound();
            }

            var lichChieu = await _context.LichChieus
                .Include(lc => lc.Phim)
                .Include(lc => lc.PhongChieu)
                    .ThenInclude(pc => pc.GheNgois)
                .FirstOrDefaultAsync(lc => lc.MaLichChieu == maLichChieu);

            if (lichChieu == null)
            {
                return NotFound();
            }

            // Lấy danh sách ghế đã được đặt cho lịch chiếu này
            var gheDaDat = await _context.Ves
                .Where(v => v.MaLichChieu == maLichChieu && v.TrangThai == "Đã đặt")
                .Select(v => v.MaGhe)
                .ToListAsync();

            var viewModel = new KhachHangChonGheViewModel
            {
                LichChieu = lichChieu,
                GheDaDat = gheDaDat
            };

            return View(viewModel);
        }

        // Thêm vé vào giỏ hàng
        [HttpPost]
        public async Task<IActionResult> ThemVaoGio([FromBody] ThemVeRequest request)
        {
            if (!IsCustomerLoggedIn())
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập" });
            }

            try
            {
                var lichChieu = await _context.LichChieus
                    .Include(lc => lc.Phim)
                    .Include(lc => lc.PhongChieu)
                    .FirstOrDefaultAsync(lc => lc.MaLichChieu == request.MaLichChieu);

                if (lichChieu == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy lịch chiếu" });
                }

                var ghe = await _context.GheNgois
                    .FirstOrDefaultAsync(g => g.MaGhe == request.MaGhe);

                if (ghe == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy ghế" });
                }

                // Kiểm tra ghế đã được đặt chưa
                var veDaDat = await _context.Ves
                    .FirstOrDefaultAsync(v => v.MaLichChieu == request.MaLichChieu && 
                                            v.MaGhe == request.MaGhe && 
                                            v.TrangThai == "Đã đặt");

                if (veDaDat != null)
                {
                    return Json(new { success = false, message = "Ghế đã được đặt" });
                }

                // Lưu vào session giỏ hàng
                var gioHang = HttpContext.Session.GetObjectFromJson<List<GioHangItem>>("GioHang") ?? new List<GioHangItem>();

                // Kiểm tra ghế đã có trong giỏ chưa
                if (gioHang.Any(item => item.MaGhe == request.MaGhe && item.MaLichChieu == request.MaLichChieu))
                {
                    return Json(new { success = false, message = "Ghế đã có trong giỏ hàng" });
                }

                var gioHangItem = new GioHangItem
                {
                    MaLichChieu = request.MaLichChieu,
                    MaGhe = request.MaGhe,
                    TenPhim = lichChieu.Phim.TenPhim,
                    TenPhong = lichChieu.PhongChieu.TenPhong,
                    SoGhe = ghe.SoGhe,
                    ThoiGianChieu = lichChieu.ThoiGianBatDau,
                    Gia = ghe.GiaGhe
                };

                gioHang.Add(gioHangItem);
                HttpContext.Session.SetObjectAsJson("GioHang", gioHang);

                return Json(new { success = true, message = "Đã thêm vé vào giỏ hàng" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // Xem giỏ hàng
        public IActionResult GioHang()
        {
            if (!IsCustomerLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            var gioHang = HttpContext.Session.GetObjectFromJson<List<GioHangItem>>("GioHang") ?? new List<GioHangItem>();
            return View(gioHang);
        }

        // Xóa vé khỏi giỏ hàng
        [HttpPost]
        public IActionResult XoaKhoiGio(string maLichChieu, string maGhe)
        {
            var gioHang = HttpContext.Session.GetObjectFromJson<List<GioHangItem>>("GioHang") ?? new List<GioHangItem>();
            
            gioHang.RemoveAll(item => item.MaLichChieu == maLichChieu && item.MaGhe == maGhe);
            
            HttpContext.Session.SetObjectAsJson("GioHang", gioHang);
            
            return RedirectToAction("GioHang");
        }

        // Thanh toán
        public async Task<IActionResult> ThanhToan()
        {
            if (!IsCustomerLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            var gioHang = HttpContext.Session.GetObjectFromJson<List<GioHangItem>>("GioHang") ?? new List<GioHangItem>();
            
            if (!gioHang.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng trống";
                return RedirectToAction("Index");
            }

            // Lấy danh sách voucher có thể sử dụng
            var vouchers = await _context.Vouchers
                .Where(v => v.ThoiGianBatDau <= DateTime.Now && v.ThoiGianKetThuc >= DateTime.Now)
                .ToListAsync();

            var viewModel = new KhachHangThanhToanViewModel
            {
                GioHang = gioHang,
                Vouchers = vouchers,
                TongTien = gioHang.Sum(item => item.Gia)
            };

            return View(viewModel);
        }

        // Xử lý thanh toán
        [HttpPost]
        public async Task<IActionResult> XuLyThanhToan(string? maVoucher)
        {
            if (!IsCustomerLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            var maKhachHang = HttpContext.Session.GetString("MaKhachHang");
            var gioHang = HttpContext.Session.GetObjectFromJson<List<GioHangItem>>("GioHang") ?? new List<GioHangItem>();

            if (!gioHang.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng trống";
                return RedirectToAction("Index");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Tạo hóa đơn
                var maHoaDon = "HD" + DateTime.Now.ToString("yyyyMMddHHmmss");
                var tongTien = gioHang.Sum(item => item.Gia);
                decimal tongTienSauGiam = tongTien;

                // Áp dụng voucher nếu có
                Voucher? voucher = null;
                if (!string.IsNullOrEmpty(maVoucher))
                {
                    voucher = await _context.Vouchers.FirstOrDefaultAsync(v => v.MaGiamGia == maVoucher);
                    if (voucher != null)
                    {
                        tongTienSauGiam = tongTien * (100 - voucher.PhanTramGiam) / 100;
                    }
                }

                var hoaDon = new HoaDon
                {
                    MaHoaDon = maHoaDon,
                    TongTien = tongTienSauGiam,
                    ThoiGianTao = DateTime.Now,
                    SoLuong = gioHang.Count,
                    MaKhachHang = maKhachHang ?? string.Empty,
                    MaNhanVien = string.Empty // Khách hàng tự thanh toán
                };

                _context.HoaDons.Add(hoaDon);

                // Tạo vé
                foreach (var item in gioHang)
                {
                    var maVe = "VE" + DateTime.Now.ToString("yyyyMMddHHmmss") + item.MaGhe;
                    
                    var ve = new Ve
                    {
                        MaVe = maVe,
                        TrangThai = "Đã đặt",
                        SoGhe = item.SoGhe,
                        TenPhim = item.TenPhim,
                        HanSuDung = item.ThoiGianChieu.AddHours(2), // Hết hạn 2h sau giờ chiếu
                        Gia = item.Gia,
                        TenPhong = item.TenPhong,
                        MaGhe = item.MaGhe,
                        MaLichChieu = item.MaLichChieu,
                        MaPhim = await _context.LichChieus
                            .Where(lc => lc.MaLichChieu == item.MaLichChieu)
                            .Select(lc => lc.MaPhim)
                            .FirstOrDefaultAsync() ?? string.Empty,
                        MaPhong = await _context.LichChieus
                            .Where(lc => lc.MaLichChieu == item.MaLichChieu)
                            .Select(lc => lc.MaPhong)
                            .FirstOrDefaultAsync() ?? string.Empty
                    };

                    _context.Ves.Add(ve);

                    // Tạo chi tiết hóa đơn
                    var cthd = new CTHD
                    {
                        MaCTHD = "CTHD" + DateTime.Now.ToString("yyyyMMddHHmmss") + item.MaGhe,
                        DonGia = item.Gia,
                        MaVe = maVe,
                        MaHoaDon = maHoaDon
                    };

                    _context.CTHDs.Add(cthd);
                }

                // Lưu voucher sử dụng nếu có
                if (voucher != null)
                {
                    var hdVoucher = new HDVoucher
                    {
                        MaHoaDon = maHoaDon,
                        MaGiamGia = voucher.MaGiamGia,
                        SoLuongVoucher = 1,
                        TongTien = tongTienSauGiam
                    };

                    _context.HDVouchers.Add(hdVoucher);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Xóa giỏ hàng
                HttpContext.Session.Remove("GioHang");

                TempData["SuccessMessage"] = "Thanh toán thành công!";
                return RedirectToAction("ThanhToanThanhCong", new { maHoaDon = maHoaDon });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi thanh toán: " + ex.Message;
                return RedirectToAction("ThanhToan");
            }
        }

        // Trang thành công
        public async Task<IActionResult> ThanhToanThanhCong(string maHoaDon)
        {
            if (!IsCustomerLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            var hoaDon = await _context.HoaDons
                .Include(hd => hd.CTHDs)
                    .ThenInclude(ct => ct.Ve)
                .FirstOrDefaultAsync(hd => hd.MaHoaDon == maHoaDon);

            if (hoaDon == null)
            {
                return NotFound();
            }

            return View(hoaDon);
        }

        // Lịch sử đặt vé
        public async Task<IActionResult> LichSuDatVe()
        {
            if (!IsCustomerLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            var maKhachHang = HttpContext.Session.GetString("MaKhachHang");
            
            var lichSu = await _context.HoaDons
                .Include(hd => hd.CTHDs)
                    .ThenInclude(ct => ct.Ve)
                        .ThenInclude(v => v.LichChieu)
                            .ThenInclude(lc => lc.Phim)
                .Where(hd => hd.MaKhachHang == maKhachHang)
                .OrderByDescending(hd => hd.ThoiGianTao)
                .ToListAsync();

            return View(lichSu);
        }

        // Quản lý tài khoản
        public async Task<IActionResult> TaiKhoan()
        {
            if (!IsCustomerLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            var maKhachHang = HttpContext.Session.GetString("MaKhachHang");
            var khachHang = await _context.KhachHangs
                .FirstOrDefaultAsync(kh => kh.MaKhachHang == maKhachHang);

            if (khachHang == null)
            {
                return NotFound();
            }

            return View(khachHang);
        }

        // Cập nhật thông tin tài khoản
        [HttpPost]
        public async Task<IActionResult> CapNhatTaiKhoan(KhachHang model)
        {
            if (!IsCustomerLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            var maKhachHang = HttpContext.Session.GetString("MaKhachHang");
            var khachHang = await _context.KhachHangs
                .FirstOrDefaultAsync(kh => kh.MaKhachHang == maKhachHang);

            if (khachHang == null)
            {
                return NotFound();
            }

            khachHang.HoTen = model.HoTen;
            khachHang.SDT = model.SDT;

            try
            {
                await _context.SaveChangesAsync();
                
                // Cập nhật session
                HttpContext.Session.SetString("TenKhachHang", khachHang.HoTen);
                
                TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
            }

            return RedirectToAction("TaiKhoan");
        }
    }
}
