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

            // Lấy lịch chiếu trong 7 ngày tới
            var lichChieuSapToi = await _context.LichChieus
                .Include(lc => lc.Phim)
                .Include(lc => lc.PhongChieu)
                .Where(lc => lc.ThoiGianBatDau >= DateTime.Now && 
                            lc.ThoiGianBatDau <= DateTime.Now.AddDays(7))
                .OrderBy(lc => lc.ThoiGianBatDau)
                .ToListAsync();

            // Chuyển đổi sang DTO để tránh vòng lặp serialization
            var lichChieuDto = lichChieuSapToi.Select(lc => new ScheduleDto
            {
                MaLichChieu = lc.MaLichChieu,
                ThoiGianBatDau = lc.ThoiGianBatDau,
                ThoiGianKetThuc = lc.ThoiGianKetThuc,
                Gia = lc.Gia,
                MaPhim = lc.MaPhim,
                Phim = new PhimDto
                {
                    MaPhim = lc.Phim.MaPhim,
                    TenPhim = lc.Phim.TenPhim,
                    TheLoai = lc.Phim.TheLoai,
                    ThoiLuong = lc.Phim.ThoiLuong,
                    DoTuoiPhanAnh = lc.Phim.DoTuoiPhanAnh
                },
                PhongChieu = new PhongChieuDto
                {
                    MaPhong = lc.PhongChieu.MaPhong,
                    TenPhong = lc.PhongChieu.TenPhong,
                    SoChoNgoi = lc.PhongChieu.SoChoNgoi
                }
            }).ToList();

            ViewBag.LichChieuSapToi = lichChieuSapToi; // Cho server-side rendering
            ViewBag.LichChieuDto = lichChieuDto; // Cho JavaScript serialization
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

            // Chuyển đổi sang object đơn giản để tránh vòng lặp serialization
            var lichChieuSimple = lichChieuTuHienTai.Select(lc => new
            {
                maLichChieu = lc.MaLichChieu,
                thoiGianBatDau = lc.ThoiGianBatDau,
                thoiGianKetThuc = lc.ThoiGianKetThuc,
                gia = lc.Gia,
                maPhim = lc.MaPhim,
                phongChieu = new
                {
                    maPhong = lc.PhongChieu.MaPhong,
                    tenPhong = lc.PhongChieu.TenPhong,
                    soChoNgoi = lc.PhongChieu.SoChoNgoi
                }
            }).ToList();

            ViewBag.LichChieus = lichChieuSimple;

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

            var danhSachGhe = await _context.GheNgois
                .Where(g => g.MaPhong == lichChieu.MaPhong)
                .OrderBy(g => g.SoGhe)
                .ToListAsync();

            var danhSachVeDaBan = await _context.Ves
                .Where(v => v.MaLichChieu == maLichChieu && v.TrangThai == "Đã bán")
                .ToListAsync();

            var danhSachVeDaPhatHanh = await _context.Ves
                .Where(v => v.MaLichChieu == maLichChieu && (v.TrangThai == "Chưa đặt" || v.TrangThai == "Còn hạn"))
                .ToListAsync();

            // Lấy danh sách ghế đã được đặt cho lịch chiếu này
            var gheDaDat = danhSachVeDaBan.Select(v => v.MaGhe).ToList();

            var viewModel = new KhachHangChonGheViewModel
            {
                LichChieu = lichChieu,
                DanhSachGhe = danhSachGhe,
                DanhSachVeDaBan = danhSachVeDaBan,
                DanhSachVeDaPhatHanh = danhSachVeDaPhatHanh,
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
                // Log request
                Console.WriteLine($"ThemVaoGio - MaLichChieu: {request.MaLichChieu}, MaGhe: {request.MaGhe}");

                var lichChieu = await _context.LichChieus
                    .Include(lc => lc.Phim)
                    .Include(lc => lc.PhongChieu)
                    .FirstOrDefaultAsync(lc => lc.MaLichChieu == request.MaLichChieu);

                if (lichChieu == null)
                {
                    Console.WriteLine($"Không tìm thấy lịch chiếu: {request.MaLichChieu}");
                    return Json(new { success = false, message = "Không tìm thấy lịch chiếu" });
                }

                var ghe = await _context.GheNgois
                    .FirstOrDefaultAsync(g => g.MaGhe == request.MaGhe);

                if (ghe == null)
                {
                    Console.WriteLine($"Không tìm thấy ghế: {request.MaGhe}");
                    return Json(new { success = false, message = "Không tìm thấy ghế" });
                }

                // Kiểm tra ghế đã được đặt chưa
                var veDaDat = await _context.Ves
                    .FirstOrDefaultAsync(v => v.MaLichChieu == request.MaLichChieu && 
                                            v.MaGhe == request.MaGhe && 
                                            v.TrangThai == "Đã đặt");

                if (veDaDat != null)
                {
                    Console.WriteLine($"Ghế đã được đặt: {request.MaGhe}");
                    return Json(new { success = false, message = "Ghế đã được đặt" });
                }

                // Lưu vào session giỏ hàng
                var gioHang = HttpContext.Session.GetObjectFromJson<List<GioHangItem>>("GioHang") ?? new List<GioHangItem>();
                Console.WriteLine($"Số item hiện tại trong giỏ hàng: {gioHang.Count}");

                // Kiểm tra ghế đã có trong giỏ chưa
                if (gioHang.Any(item => item.MaGhe == request.MaGhe && item.MaLichChieu == request.MaLichChieu))
                {
                    Console.WriteLine($"Ghế đã có trong giỏ hàng: {request.MaGhe}");
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
                
                Console.WriteLine($"Đã thêm ghế {ghe.SoGhe} vào giỏ hàng. Tổng số item: {gioHang.Count}");

                return Json(new { success = true, message = "Đã thêm vé vào giỏ hàng" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi ThemVaoGio: {ex.Message}");
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
                // Tạo mã hóa đơn unique
                var lastHoaDon = await _context.HoaDons
                    .OrderByDescending(h => h.MaHoaDon)
                    .FirstOrDefaultAsync();

                var soThuTu = 1;
                if (lastHoaDon != null && lastHoaDon.MaHoaDon.StartsWith("HD"))
                {
                    if (int.TryParse(lastHoaDon.MaHoaDon.Substring(2), out var soHienTai))
                    {
                        soThuTu = soHienTai + 1;
                    }
                }
                var maHoaDon = $"HD{soThuTu:D3}";

                var tongTien = gioHang.Sum(item => item.Gia);
                decimal tienGiamGia = 0;
                decimal tongTienSauGiam = tongTien;

                // Áp dụng voucher nếu có
                Voucher? voucher = null;
                if (!string.IsNullOrEmpty(maVoucher))
                {
                    voucher = await _context.Vouchers
                        .FirstOrDefaultAsync(v => v.MaGiamGia == maVoucher && 
                                                 v.ThoiGianBatDau <= DateTime.Now && 
                                                 v.ThoiGianKetThuc >= DateTime.Now);
                    if (voucher != null)
                    {
                        tienGiamGia = tongTien * voucher.PhanTramGiam / 100;
                        tongTienSauGiam = tongTien - tienGiamGia;
                    }
                }

                var hoaDon = new HoaDon
                {
                    MaHoaDon = maHoaDon,
                    TongTien = tongTienSauGiam,
                    ThoiGianTao = DateTime.Now,
                    SoLuong = gioHang.Count,
                    MaKhachHang = maKhachHang ?? string.Empty,
                    MaNhanVien = "GUEST" // Khách hàng tự thanh toán
                };

                _context.HoaDons.Add(hoaDon);

                // Tạo vé và chi tiết hóa đơn
                var soThuTuVe = 1;
                var lastVe = await _context.Ves.OrderByDescending(v => v.MaVe).FirstOrDefaultAsync();
                if (lastVe != null && lastVe.MaVe.StartsWith("VE"))
                {
                    if (int.TryParse(lastVe.MaVe.Substring(2), out var soHienTaiVe))
                    {
                        soThuTuVe = soHienTaiVe + 1;
                    }
                }

                var soThuTuCTHD = 1;
                var lastCTHD = await _context.CTHDs.OrderByDescending(c => c.MaCTHD).FirstOrDefaultAsync();
                if (lastCTHD != null && lastCTHD.MaCTHD.StartsWith("CT"))
                {
                    if (int.TryParse(lastCTHD.MaCTHD.Substring(2), out var soHienTaiCTHD))
                    {
                        soThuTuCTHD = soHienTaiCTHD + 1;
                    }
                }

                foreach (var item in gioHang)
                {
                    // Kiểm tra ghế có bị đặt trong thời gian này không
                    var gheExist = await _context.Ves
                        .FirstOrDefaultAsync(v => v.MaLichChieu == item.MaLichChieu && 
                                                 v.MaGhe == item.MaGhe && 
                                                 v.TrangThai == "Đã đặt");
                    
                    if (gheExist != null)
                    {
                        throw new Exception($"Ghế {item.SoGhe} đã được đặt bởi khách hàng khác");
                    }

                    var maVe = $"VE{soThuTuVe:D3}";
                    soThuTuVe++;
                    
                    var ve = new Ve
                    {
                        MaVe = maVe,
                        TrangThai = "Đã bán",
                        SoGhe = item.SoGhe,
                        TenPhim = item.TenPhim,
                        HanSuDung = item.ThoiGianChieu.AddHours(2),
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
                    var maCTHD = $"CT{soThuTuCTHD:D3}";
                    soThuTuCTHD++;

                    var cthd = new CTHD
                    {
                        MaCTHD = maCTHD,
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

                // Cập nhật điểm tích lũy cho khách hàng
                if (!string.IsNullOrEmpty(maKhachHang))
                {
                    var khachHang = await _context.KhachHangs.FindAsync(maKhachHang);
                    if (khachHang != null)
                    {
                        var diemTichLuyMoi = (int)(tongTienSauGiam / 10000); // 1 điểm = 10,000 VNĐ
                        khachHang.DiemTichLuy += diemTichLuyMoi;
                        _context.KhachHangs.Update(khachHang);

                        Console.WriteLine($"Cập nhật điểm tích lũy cho KH {maKhachHang}: +{diemTichLuyMoi} điểm");
                    }
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
                Console.WriteLine($"Lỗi thanh toán: {ex.Message}");
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

            if (string.IsNullOrEmpty(maHoaDon))
            {
                return RedirectToAction("Index");
            }

            var hoaDon = await _context.HoaDons
                .Include(hd => hd.KhachHang)
                .FirstOrDefaultAsync(hd => hd.MaHoaDon == maHoaDon);

            if (hoaDon == null)
            {
                return NotFound();
            }

            // Lấy chi tiết hóa đơn và vé
            var chiTietHoaDon = await _context.CTHDs
                .Include(ct => ct.Ve)
                .Where(ct => ct.MaHoaDon == maHoaDon)
                .ToListAsync();

            // Lấy thông tin lịch chiếu cho từng vé
            var chiTietVe = new List<VeChiTietViewModel>();
            foreach (var cthd in chiTietHoaDon)
            {
                var ve = cthd.Ve;
                if (ve != null)
                {
                    // Lấy thông tin lịch chiếu
                    var lichChieu = await _context.LichChieus
                        .FirstOrDefaultAsync(lc => lc.MaLichChieu == ve.MaLichChieu);

                    chiTietVe.Add(new VeChiTietViewModel
                    {
                        MaVe = ve.MaVe,
                        TenPhim = ve.TenPhim,
                        TenPhong = ve.TenPhong,
                        SoGhe = ve.SoGhe,
                        ThoiGianChieu = lichChieu?.ThoiGianBatDau ?? DateTime.Now,
                        HanSuDung = ve.HanSuDung,
                        Gia = ve.Gia,
                        TrangThai = ve.TrangThai
                    });
                }
            }

            // Lấy thông tin voucher nếu có
            var hdVoucher = await _context.HDVouchers
                .Include(hv => hv.Voucher)
                .FirstOrDefaultAsync(hv => hv.MaHoaDon == maHoaDon);

            // Tính điểm tích lũy nhận được
            var diemTichLuyNhan = (int)(hoaDon.TongTien / 10000);

            var viewModel = new ThanhToanThanhCongViewModel
            {
                HoaDon = hoaDon,
                ChiTietVe = chiTietVe,
                VoucherSuDung = hdVoucher?.Voucher,
                TienGiamGia = hdVoucher?.TongTien ?? 0,
                KhachHang = hoaDon.KhachHang,
                DiemTichLuyNhan = diemTichLuyNhan
            };

            return View(viewModel);
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

        // API để lấy thống kê khách hàng
        [HttpGet]
        public async Task<IActionResult> GetThongKeKhachHang()
        {
            if (!IsCustomerLoggedIn())
            {
                return Json(new { success = false, message = "Chưa đăng nhập" });
            }

            var maKhachHang = HttpContext.Session.GetString("MaKhachHang");
            
            try
            {
                // Lấy tổng số vé đã mua thông qua HoaDon -> CTHD -> Ve
                var tongSoVe = await _context.CTHDs
                    .Where(cthd => cthd.HoaDon.MaKhachHang == maKhachHang)
                    .CountAsync();

                // Lấy tổng số tiền đã chi tiêu
                var tongChiTieu = await _context.HoaDons
                    .Where(hd => hd.MaKhachHang == maKhachHang)
                    .SumAsync(hd => hd.TongTien);

                // Lấy thể loại phim yêu thích (thể loại được mua nhiều nhất)
                var theLoaiYeuThich = await _context.CTHDs
                    .Where(cthd => cthd.HoaDon.MaKhachHang == maKhachHang)
                    .Include(cthd => cthd.Ve)
                    .ThenInclude(v => v.Phim)
                    .GroupBy(cthd => cthd.Ve.Phim.TheLoai)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .FirstOrDefaultAsync() ?? "Chưa có";

                // Lấy thông tin khách hàng để có điểm tích lũy
                var khachHang = await _context.KhachHangs
                    .FirstOrDefaultAsync(kh => kh.MaKhachHang == maKhachHang);

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        tongSoVe = tongSoVe,
                        tongChiTieu = tongChiTieu,
                        theLoaiYeuThich = theLoaiYeuThich,
                        diemTichLuy = khachHang?.DiemTichLuy ?? 0
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // API để lấy trạng thái ghế real-time
        [HttpGet]
        public async Task<IActionResult> GetTrangThaiGhe(string maLichChieu)
        {
            if (!IsCustomerLoggedIn())
            {
                return Json(new { success = false, message = "Chưa đăng nhập" });
            }

            try
            {
                var danhSachVeDaBan = await _context.Ves
                    .Where(v => v.MaLichChieu == maLichChieu && v.TrangThai == "Đã bán")
                    .Select(v => v.MaGhe)
                    .ToListAsync();

                var danhSachVeDaPhatHanh = await _context.Ves
                    .Where(v => v.MaLichChieu == maLichChieu && (v.TrangThai == "Chưa đặt" || v.TrangThai == "Còn hạn"))
                    .Select(v => v.MaGhe)
                    .ToListAsync();

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        gheDaBan = danhSachVeDaBan,
                        gheDaPhatHanh = danhSachVeDaPhatHanh
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
