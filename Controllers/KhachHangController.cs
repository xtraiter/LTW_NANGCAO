using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CinemaManagement.Data;
using CinemaManagement.Models;
using CinemaManagement.ViewModels;
using CinemaManagement.Extensions;
using System.Security.Cryptography;
using System.Text;

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

        // Trang chủ khách hàng - Hiển thị danh sách phim với bộ lọc nâng cao
        public async Task<IActionResult> Index(
            string? theLoai, 
            string? searchTerm,
            string? doTuoi,
            string? thoiLuongFilter,
            string? giaFilter,
            decimal? ratingFilter,
            string? sortBy = "name_asc")
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

            // Lọc theo độ tuổi
            if (!string.IsNullOrEmpty(doTuoi))
            {
                phims = phims.Where(p => p.DoTuoiPhanAnh == doTuoi);
            }

            // Lọc theo thời lượng
            if (!string.IsNullOrEmpty(thoiLuongFilter))
            {
                switch (thoiLuongFilter)
                {
                    case "short":
                        phims = phims.Where(p => p.ThoiLuong <= 90);
                        break;
                    case "medium":
                        phims = phims.Where(p => p.ThoiLuong > 90 && p.ThoiLuong <= 150);
                        break;
                    case "long":
                        phims = phims.Where(p => p.ThoiLuong > 150);
                        break;
                }
            }

            var phimList = await phims.ToListAsync();

            // Lấy thông tin giá vé trung bình cho mỗi phim
            var phimWithPrice = new List<dynamic>();
            foreach (var phim in phimList)
            {
                var avgPrice = await _context.LichChieus
                    .Where(lc => lc.MaPhim == phim.MaPhim)
                    .AverageAsync(lc => (decimal?)lc.Gia) ?? 0;

                var avgRating = await _context.DanhGiaPhims
                    .Where(dg => dg.MaPhim == phim.MaPhim)
                    .AverageAsync(dg => (decimal?)dg.DiemDanhGia) ?? 0;

                var soldTickets = await _context.CTHDs
                    .Include(ct => ct.Ve)
                    .Where(ct => ct.Ve.MaPhim == phim.MaPhim)
                    .CountAsync();

                phimWithPrice.Add(new
                {
                    Phim = phim,
                    AvgPrice = avgPrice,
                    AvgRating = avgRating,
                    SoldTickets = soldTickets
                });
            }

            // Lọc theo giá
            if (!string.IsNullOrEmpty(giaFilter))
            {
                switch (giaFilter)
                {
                    case "budget":
                        phimWithPrice = phimWithPrice.Where(p => ((decimal)p.AvgPrice) <= 80000).ToList();
                        break;
                    case "medium":
                        phimWithPrice = phimWithPrice.Where(p => ((decimal)p.AvgPrice) > 80000 && ((decimal)p.AvgPrice) <= 150000).ToList();
                        break;
                    case "premium":
                        phimWithPrice = phimWithPrice.Where(p => ((decimal)p.AvgPrice) > 150000).ToList();
                        break;
                }
            }

            // Lọc theo đánh giá
            if (ratingFilter.HasValue)
            {
                phimWithPrice = phimWithPrice.Where(p => ((decimal)p.AvgRating) >= ratingFilter.Value).ToList();
            }

            // Sắp xếp
            switch (sortBy?.ToLower())
            {
                case "name_desc":
                    phimWithPrice = phimWithPrice.OrderByDescending(p => ((Phim)p.Phim).TenPhim).ToList();
                    break;
                case "rating_desc":
                    phimWithPrice = phimWithPrice.OrderByDescending(p => (decimal)p.AvgRating).ToList();
                    break;
                case "rating_asc":
                    phimWithPrice = phimWithPrice.OrderBy(p => (decimal)p.AvgRating).ToList();
                    break;
                case "duration_desc":
                    phimWithPrice = phimWithPrice.OrderByDescending(p => ((Phim)p.Phim).ThoiLuong).ToList();
                    break;
                case "duration_asc":
                    phimWithPrice = phimWithPrice.OrderBy(p => ((Phim)p.Phim).ThoiLuong).ToList();
                    break;
                case "price_desc":
                    phimWithPrice = phimWithPrice.OrderByDescending(p => (decimal)p.AvgPrice).ToList();
                    break;
                case "price_asc":
                    phimWithPrice = phimWithPrice.OrderBy(p => (decimal)p.AvgPrice).ToList();
                    break;
                case "popularity_desc":
                    phimWithPrice = phimWithPrice.OrderByDescending(p => (int)p.SoldTickets).ToList();
                    break;
                default: // name_asc
                    phimWithPrice = phimWithPrice.OrderBy(p => ((Phim)p.Phim).TenPhim).ToList();
                    break;
            }

            // Chuyển đổi lại về danh sách Phim
            phimList = phimWithPrice.Select(p => (Phim)p.Phim).ToList();

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
            
            // Current filter values
            ViewBag.CurrentTheLoai = theLoai;
            ViewBag.CurrentSearch = searchTerm;
            ViewBag.CurrentDoTuoi = doTuoi;
            ViewBag.CurrentThoiLuongFilter = thoiLuongFilter;
            ViewBag.CurrentGiaFilter = giaFilter;
            ViewBag.CurrentRatingFilter = ratingFilter;
            ViewBag.CurrentSortBy = sortBy;

            // Filter options
            ViewBag.DoTuoiOptions = await _context.Phims
                .Select(p => p.DoTuoiPhanAnh)
                .Distinct()
                .OrderBy(d => d)
                .ToListAsync();

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

            // Lấy thông tin đánh giá phim
            var danhGiaPhim = await _context.DanhGiaPhims
                .Include(dg => dg.KhachHang)
                .Where(dg => dg.MaPhim == id)
                .OrderByDescending(dg => dg.ThoiGianDanhGia)
                .ToListAsync();

            // Tính điểm trung bình
            var diemTrungBinh = danhGiaPhim.Any() ? Math.Round(danhGiaPhim.Average(dg => dg.DiemDanhGia), 1) : 0;
            var tongSoDanhGia = danhGiaPhim.Count;

            ViewBag.DiemTrungBinh = diemTrungBinh;
            ViewBag.TongSoDanhGia = tongSoDanhGia;
            ViewBag.DanhGiaPhim = danhGiaPhim;

            // Lấy danh sách phim đề xuất/liên quan
            var phimLienQuan = await GetRecommendedMovies(phim, 8);
            ViewBag.PhimLienQuan = phimLienQuan;

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

        // Thanh toán - GET method (chuyển hướng về trang chủ vì không dùng giỏ hàng)
        public IActionResult ThanhToan()
        {
            if (!IsCustomerLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            // Chuyển hướng về trang chủ vì đã không sử dụng giỏ hàng
            TempData["ErrorMessage"] = "Vui lòng chọn ghế từ trang lịch chiếu để thanh toán";
            return RedirectToAction("Index");
        }

        // Thanh toán trực tiếp từ trang chọn ghế - POST method
        [HttpPost]
        public async Task<IActionResult> ThanhToan(string maLichChieu, List<SelectedSeatViewModel> selectedSeats, decimal tongTien)
        {
            if (!IsCustomerLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            // Log để debug
            Console.WriteLine($"=== NHẬN DỮ LIỆU THANH TOÁN ===");
            Console.WriteLine($"Mã lịch chiếu: {maLichChieu}");
            Console.WriteLine($"Số ghế: {selectedSeats?.Count ?? 0}");
            Console.WriteLine($"Tổng tiền: {tongTien}");

            if (selectedSeats == null || !selectedSeats.Any())
            {
                TempData["ErrorMessage"] = "Không có ghế nào được chọn";
                return RedirectToAction("ChonGhe", new { maLichChieu });
            }

            // Kiểm tra lịch chiếu tồn tại
            var lichChieu = await _context.LichChieus
                .Include(lc => lc.Phim)
                .Include(lc => lc.PhongChieu)
                .FirstOrDefaultAsync(lc => lc.MaLichChieu == maLichChieu);

            if (lichChieu == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy lịch chiếu";
                return RedirectToAction("Index");
            }

            // Tạo danh sách ghế cho giỏ hàng tạm thời
            var gioHangItems = new List<GioHangItem>();
            foreach (var seat in selectedSeats)
            {
                var ghe = await _context.GheNgois
                    .FirstOrDefaultAsync(g => g.MaGhe == seat.MaGhe);

                if (ghe != null)
                {
                    gioHangItems.Add(new GioHangItem
                    {
                        MaLichChieu = maLichChieu,
                        MaGhe = seat.MaGhe,
                        SoGhe = seat.SoGhe,
                        Gia = seat.GiaGhe,
                        TenPhim = lichChieu.Phim.TenPhim,
                        ThoiGianChieu = lichChieu.ThoiGianBatDau,
                        TenPhong = lichChieu.PhongChieu.TenPhong,
                        PhongChieu = lichChieu.PhongChieu.TenPhong
                    });
                }
            }

            // Lưu vào session để sử dụng trong trang thanh toán
            HttpContext.Session.SetObjectAsJson("TempGioHang", gioHangItems);

            // Lấy danh sách voucher có thể sử dụng
            var vouchers = await _context.Vouchers
                .Where(v => v.ThoiGianBatDau <= DateTime.Now && v.ThoiGianKetThuc >= DateTime.Now)
                .ToListAsync();

            var maKhachHang = HttpContext.Session.GetString("MaKhachHang");
            var khachHang = await _context.KhachHangs.FirstOrDefaultAsync(kh => kh.MaKhachHang == maKhachHang);
            int diemTichLuy = khachHang?.DiemTichLuy ?? 0;

            var viewModel = new KhachHangThanhToanViewModel
            {
                GioHang = gioHangItems,
                Vouchers = vouchers,
                TongTien = tongTien,
                IsDirectPayment = true, // Flag để biết đây là thanh toán trực tiếp
                DiemTichLuy = diemTichLuy
            };

            return View(viewModel);
        }

        // Xử lý thanh toán
        [HttpPost]
        public async Task<IActionResult> XuLyThanhToan(string? maVoucher)
        {
            Console.WriteLine("=== BẮT ĐẦU XỬ LÝ THANH TOÁN ===");
            Console.WriteLine($"Thời gian: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"Mã voucher nhận được: '{maVoucher}'");

            if (!IsCustomerLoggedIn())
            {
                Console.WriteLine("CẢNH BÁO: Khách hàng chưa đăng nhập");
                return RedirectToAction("Login", "Auth");
            }

            var maKhachHang = HttpContext.Session.GetString("MaKhachHang");
            Console.WriteLine($"Mã khách hàng: '{maKhachHang}'");

            // Chỉ sử dụng giỏ hàng tạm thời từ thanh toán trực tiếp
            var gioHang = HttpContext.Session.GetObjectFromJson<List<GioHangItem>>("TempGioHang") ?? new List<GioHangItem>();

            Console.WriteLine($"Kiểm tra dữ liệu giỏ hàng:");
            Console.WriteLine($"- TempGioHang items: {gioHang.Count}");
            Console.WriteLine($"- Sử dụng giỏ hàng tạm thời với {gioHang.Count} items");

            if (!gioHang.Any())
            {
                Console.WriteLine("LỖI: Giỏ hàng trống - chuyển hướng về trang chủ");
                TempData["ErrorMessage"] = "Giỏ hàng trống";
                return RedirectToAction("Index");
            }

            // Log chi tiết từng item trong giỏ hàng
            Console.WriteLine("Chi tiết giỏ hàng:");
            for (int i = 0; i < gioHang.Count; i++)
            {
                var item = gioHang[i];
                Console.WriteLine($"  Item {i + 1}: Ghế {item.SoGhe}, Phim: {item.TenPhim}, Giá: {item.Gia:N0}, Lịch chiếu: {item.MaLichChieu}");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            Console.WriteLine("Đã bắt đầu transaction database");
            
            try
            {
                // Tạo mã hóa đơn unique
                Console.WriteLine("Bước 1: Tạo mã hóa đơn");
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
                Console.WriteLine($"Mã hóa đơn được tạo: {maHoaDon}");

                var tongTien = gioHang.Sum(item => item.Gia);
                Console.WriteLine($"Tổng tiền gốc: {tongTien:N0} VNĐ");

                decimal tienGiamGia = 0;
                decimal tongTienSauGiam = tongTien;

                // Áp dụng voucher nếu có
                Console.WriteLine("Bước 2: Xử lý voucher");
                Voucher? voucher = null;
                if (!string.IsNullOrEmpty(maVoucher))
                {
                    Console.WriteLine($"Tìm kiếm voucher: {maVoucher}");
                    voucher = await _context.Vouchers
                        .FirstOrDefaultAsync(v => v.MaGiamGia == maVoucher && 
                                                 v.ThoiGianBatDau <= DateTime.Now && 
                                                 v.ThoiGianKetThuc >= DateTime.Now);
                    if (voucher != null)
                    {
                        tienGiamGia = tongTien * voucher.PhanTramGiam / 100;
                        tongTienSauGiam = tongTien - tienGiamGia;
                        Console.WriteLine($"Voucher hợp lệ: Giảm {voucher.PhanTramGiam}% = {tienGiamGia:N0} VNĐ");
                        Console.WriteLine($"Tổng tiền sau giảm: {tongTienSauGiam:N0} VNĐ");
                    }
                    else
                    {
                        Console.WriteLine("Voucher không hợp lệ hoặc hết hạn");
                    }
                }
                else
                {
                    Console.WriteLine("Không sử dụng voucher");
                }

                Console.WriteLine("Bước 3: Tạo hóa đơn");
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
                Console.WriteLine($"Đã thêm hóa đơn vào context: {maHoaDon}, Số lượng: {gioHang.Count}, Tổng tiền: {tongTienSauGiam:N0}");

                // Tạo vé và chi tiết hóa đơn
                Console.WriteLine("Bước 4: Tạo vé và chi tiết hóa đơn");
                var soThuTuVe = 1;
                var lastVe = await _context.Ves.OrderByDescending(v => v.MaVe).FirstOrDefaultAsync();
                if (lastVe != null && lastVe.MaVe.StartsWith("VE"))
                {
                    if (int.TryParse(lastVe.MaVe.Substring(2), out var soHienTaiVe))
                    {
                        soThuTuVe = soHienTaiVe + 1;
                    }
                }
                Console.WriteLine($"Bắt đầu tạo vé từ số thứ tự: {soThuTuVe}");

                var soThuTuCTHD = 1;
                var lastCTHD = await _context.CTHDs.OrderByDescending(c => c.MaCTHD).FirstOrDefaultAsync();
                if (lastCTHD != null && lastCTHD.MaCTHD.StartsWith("CT"))
                {
                    if (int.TryParse(lastCTHD.MaCTHD.Substring(2), out var soHienTaiCTHD))
                    {
                        soThuTuCTHD = soHienTaiCTHD + 1;
                    }
                }
                Console.WriteLine($"Bắt đầu tạo CTHD từ số thứ tự: {soThuTuCTHD}");

                foreach (var item in gioHang)
                {
                    Console.WriteLine($"Xử lý ghế: {item.SoGhe} - Lịch chiếu: {item.MaLichChieu}");
                    
                    // Kiểm tra ghế có bị đặt trong thời gian này không
                    var gheExist = await _context.Ves
                        .FirstOrDefaultAsync(v => v.MaLichChieu == item.MaLichChieu && 
                                                 v.MaGhe == item.MaGhe && 
                                                 v.TrangThai == "Đã đặt");
                    
                    if (gheExist != null)
                    {
                        Console.WriteLine($"LỖI: Ghế {item.SoGhe} đã bị đặt (MaVe: {gheExist.MaVe})");
                        throw new Exception($"Ghế {item.SoGhe} đã được đặt bởi khách hàng khác");
                    }

                    var maVe = $"VE{soThuTuVe:D3}";
                    Console.WriteLine($"Tạo vé: {maVe} cho ghế {item.SoGhe}");
                    soThuTuVe++;
                    
                    // Lấy thông tin phim và phòng từ lịch chiếu
                    var lichChieuInfo = await _context.LichChieus
                        .Include(lc => lc.Phim)
                        .Include(lc => lc.PhongChieu)
                        .FirstOrDefaultAsync(lc => lc.MaLichChieu == item.MaLichChieu);

                    if (lichChieuInfo == null)
                    {
                        Console.WriteLine($"LỖI: Không tìm thấy lịch chiếu {item.MaLichChieu}");
                        throw new Exception($"Không tìm thấy lịch chiếu {item.MaLichChieu}");
                    }

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
                        MaPhim = lichChieuInfo.MaPhim,
                        MaPhong = lichChieuInfo.MaPhong
                    };

                    _context.Ves.Add(ve);
                    Console.WriteLine($"Đã thêm vé {maVe} vào context");

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
                    Console.WriteLine($"Đã thêm CTHD {maCTHD} vào context (Vé: {maVe}, HĐ: {maHoaDon}, Giá: {item.Gia:N0})");
                }

                // Lưu voucher sử dụng nếu có
                Console.WriteLine("Bước 5: Xử lý voucher sử dụng");
                if (voucher != null)
                {
                    Console.WriteLine($"Lưu thông tin sử dụng voucher: {voucher.MaGiamGia}");
                    var hdVoucher = new HDVoucher
                    {
                        MaHoaDon = maHoaDon,
                        MaGiamGia = voucher.MaGiamGia,
                        SoLuongVoucher = 1,
                        TongTien = tongTienSauGiam
                    };

                    _context.HDVouchers.Add(hdVoucher);
                    Console.WriteLine($"Đã thêm HDVoucher vào context");
                }

                // Cập nhật điểm tích lũy cho khách hàng
                Console.WriteLine("Bước 6: Cập nhật điểm tích lũy");
                if (!string.IsNullOrEmpty(maKhachHang))
                {
                    var khachHang = await _context.KhachHangs.FindAsync(maKhachHang);
                    if (khachHang != null)
                    {
                        var diemTichLuyMoi = (int)(tongTienSauGiam / 10000); // 1 điểm = 10,000 VNĐ
                        var diemCu = khachHang.DiemTichLuy;
                        khachHang.DiemTichLuy += diemTichLuyMoi;
                        _context.KhachHangs.Update(khachHang);

                        Console.WriteLine($"Cập nhật điểm tích lũy cho KH {maKhachHang}: {diemCu} → {khachHang.DiemTichLuy} (+{diemTichLuyMoi} điểm)");
                    }
                    else
                    {
                        Console.WriteLine($"CẢNH BÁO: Không tìm thấy khách hàng {maKhachHang}");
                    }
                }

                Console.WriteLine("Bước 7: Lưu tất cả thay đổi vào database");
                await _context.SaveChangesAsync();
                Console.WriteLine("Đã lưu thành công vào database");

                Console.WriteLine("Bước 8: Commit transaction");
                await transaction.CommitAsync();
                Console.WriteLine("Transaction đã được commit thành công");

                // Xóa giỏ hàng tạm thời
                Console.WriteLine("Bước 9: Dọn dẹp session");
                HttpContext.Session.Remove("TempGioHang");
                Console.WriteLine("Đã xóa giỏ hàng tạm thời khỏi session");

                Console.WriteLine($"=== THANH TOÁN THÀNH CÔNG ===");
                Console.WriteLine($"Mã hóa đơn: {maHoaDon}");
                Console.WriteLine($"Tổng tiền: {tongTienSauGiam:N0} VNĐ");
                Console.WriteLine($"Số vé: {gioHang.Count}");

                TempData["SuccessMessage"] = "Thanh toán thành công!";
                return RedirectToAction("ThanhToanThanhCong", new { maHoaDon = maHoaDon });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== LỖI TRONG QUÁ TRÌNH THANH TOÁN ===");
                Console.WriteLine($"Thời gian lỗi: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"Loại lỗi: {ex.GetType().Name}");
                Console.WriteLine($"Thông báo lỗi: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }

                try
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine("Transaction đã được rollback");
                }
                catch (Exception rollbackEx)
                {
                    Console.WriteLine($"Lỗi khi rollback transaction: {rollbackEx.Message}");
                }

                TempData["ErrorMessage"] = "Có lỗi xảy ra khi thanh toán: " + ex.Message;
                Console.WriteLine($"Chuyển hướng về trang thanh toán với thông báo lỗi");
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

            // Lấy thông tin tài khoản
            var taiKhoan = await _context.TaiKhoans
                .FirstOrDefaultAsync(tk => tk.MaKhachHang == maKhachHang);

            // Kiểm tra xem có phải tài khoản Google không (mật khẩu có độ dài 64 ký tự - SHA256)
            bool isGoogleAccount = taiKhoan != null && taiKhoan.MatKhau.Length == 64;

            // Lấy thống kê
            var hoaDons = await _context.HoaDons
                .Include(h => h.CTHDs)
                .ThenInclude(c => c.Ve)
                .ThenInclude(v => v.LichChieu)
                .ThenInclude(l => l.Phim)
                .Where(h => h.MaKhachHang == maKhachHang)
                .OrderByDescending(h => h.ThoiGianTao)
                .ToListAsync();

            var tongChiTieu = hoaDons.Sum(h => h.TongTien);

            var lichSuGanDay = hoaDons.Take(5).ToList();

            var viewModel = new KhachHangProfileViewModel
            {
                KhachHang = khachHang,
                TaiKhoan = taiKhoan!,
                TongChiTieu = tongChiTieu,
                LichSuGanDay = lichSuGanDay,
                IsGoogleAccount = isGoogleAccount
            };

            return View(viewModel);
        }

        // Cập nhật thông tin tài khoản
        [HttpPost]
        public async Task<IActionResult> CapNhatTaiKhoan(UpdateProfileViewModel model)
        {
            if (!IsCustomerLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Thông tin không hợp lệ!";
                return RedirectToAction("TaiKhoan");
            }

            var maKhachHang = HttpContext.Session.GetString("MaKhachHang");
            var khachHang = await _context.KhachHangs
                .FirstOrDefaultAsync(kh => kh.MaKhachHang == maKhachHang);

            if (khachHang == null)
            {
                return NotFound();
            }

            // Kiểm tra email có bị trùng không (nếu thay đổi email)
            var taiKhoan = await _context.TaiKhoans
                .FirstOrDefaultAsync(tk => tk.MaKhachHang == maKhachHang);

            // Không cho phép thay đổi email
            // if (taiKhoan != null && taiKhoan.Email != model.Email)
            // {
            //     var existingEmail = await _context.TaiKhoans
            //         .AnyAsync(tk => tk.Email == model.Email && tk.MaTK != taiKhoan.MaTK);

            //     if (existingEmail)
            //     {
            //         TempData["ErrorMessage"] = "Email đã được sử dụng bởi tài khoản khác!";
            //         return RedirectToAction("TaiKhoan");
            //     }

            //     taiKhoan.Email = model.Email;
            //     HttpContext.Session.SetString("Email", model.Email);
            // }

            khachHang.HoTen = model.HoTen;
            khachHang.SDT = model.SDT;
            khachHang.DiaChiGiaoHang = model.DiaChiGiaoHang;

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

        // Đổi mật khẩu
        [HttpPost]
        public async Task<IActionResult> DoiMatKhau(ChangePasswordViewModel model)
        {
            if (!IsCustomerLoggedIn())
            {
                return Json(new { success = false, message = "Chưa đăng nhập" });
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return Json(new { success = false, message = string.Join(", ", errors) });
            }

            var maKhachHang = HttpContext.Session.GetString("MaKhachHang");
            var taiKhoan = await _context.TaiKhoans
                .FirstOrDefaultAsync(tk => tk.MaKhachHang == maKhachHang);

            if (taiKhoan == null)
            {
                return Json(new { success = false, message = "Không tìm thấy tài khoản" });
            }

            // Kiểm tra mật khẩu hiện tại
            if (!VerifyPassword(model.CurrentPassword, taiKhoan.MatKhau))
            {
                return Json(new { success = false, message = "Mật khẩu hiện tại không đúng" });
            }

            try
            {
                taiKhoan.MatKhau = HashPassword(model.NewPassword);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đổi mật khẩu thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // Chi tiết hóa đơn
        public async Task<IActionResult> ChiTietHoaDon(string id)
        {
            if (!IsCustomerLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            var maKhachHang = HttpContext.Session.GetString("MaKhachHang");
            var hoaDon = await _context.HoaDons
                .Include(h => h.CTHDs)
                .ThenInclude(c => c.Ve)
                .ThenInclude(v => v.LichChieu)
                .ThenInclude(l => l.Phim)
                .Include(h => h.CTHDs)
                .ThenInclude(c => c.Ve)
                .ThenInclude(v => v.LichChieu)
                .ThenInclude(l => l.PhongChieu)
                .Include(h => h.CTHDs)
                .ThenInclude(c => c.Ve)
                .ThenInclude(v => v.GheNgoi)
                .Include(h => h.HDVouchers)
                .ThenInclude(hv => hv.Voucher)
                .FirstOrDefaultAsync(h => h.MaHoaDon == id && h.MaKhachHang == maKhachHang);

            if (hoaDon == null)
            {
                return NotFound();
            }

            return View(hoaDon);
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
                // Lấy tổng số tiền đã chi tiêu
                var tongChiTieu = await _context.HoaDons
                    .Where(hd => hd.MaKhachHang == maKhachHang)
                    .SumAsync(hd => hd.TongTien);

                // Lấy thông tin khách hàng để có điểm tích lũy
                var khachHang = await _context.KhachHangs
                    .FirstOrDefaultAsync(kh => kh.MaKhachHang == maKhachHang);

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        tongChiTieu = tongChiTieu,
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

        // Helper methods for password handling
        private string HashPassword(string password)
        {
            // Đây là implementation đơn giản, trong thực tế nên dùng bcrypt hoặc tương tự
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        // Đánh giá phim
        [HttpGet]
        public async Task<IActionResult> DanhGiaPhim(string id)
        {
            if (!IsCustomerLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            var maKhachHang = HttpContext.Session.GetString("MaKhachHang");
            
            var phim = await _context.Phims
                .Include(p => p.DanhGiaPhims)
                .ThenInclude(d => d.KhachHang)
                .FirstOrDefaultAsync(p => p.MaPhim == id);

            if (phim == null)
            {
                return NotFound();
            }

            var danhGiaCuaToi = await _context.DanhGiaPhims
                .FirstOrDefaultAsync(d => d.MaPhim == id && d.MaKhachHang == maKhachHang);

            var viewModel = new PhimRatingViewModel
            {
                MaPhim = phim.MaPhim,
                TenPhim = phim.TenPhim,
                DiemTrungBinh = phim.DanhGiaPhims.Any() ? phim.DanhGiaPhims.Average(d => d.DiemDanhGia) : 0,
                TongSoDanhGia = phim.DanhGiaPhims.Count,
                DaDanhGia = danhGiaCuaToi != null,
                DanhGiaCuaToi = danhGiaCuaToi != null ? new DanhGiaPhimViewModel
                {
                    DiemDanhGia = danhGiaCuaToi.DiemDanhGia,
                    NoiDungDanhGia = danhGiaCuaToi.NoiDungDanhGia,
                    MaPhim = id
                } : null,
                DanhSachDanhGia = phim.DanhGiaPhims
                    .OrderByDescending(d => d.ThoiGianDanhGia)
                    .Take(10)
                    .Select(d => new DanhGiaChiTietViewModel
                    {
                        MaDanhGia = d.MaDanhGia,
                        DiemDanhGia = d.DiemDanhGia,
                        NoiDungDanhGia = d.NoiDungDanhGia,
                        ThoiGianDanhGia = d.ThoiGianDanhGia,
                        TenKhachHang = d.KhachHang.HoTen
                    }).ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> DanhGiaPhim(DanhGiaPhimViewModel model)
        {
            if (!IsCustomerLoggedIn())
            {
                return Json(new { success = false, message = "Chưa đăng nhập" });
            }

            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
            }

            var maKhachHang = HttpContext.Session.GetString("MaKhachHang");

            try
            {
                // Kiểm tra xem đã đánh giá chưa
                var danhGiaCu = await _context.DanhGiaPhims
                    .FirstOrDefaultAsync(d => d.MaPhim == model.MaPhim && d.MaKhachHang == maKhachHang);

                if (danhGiaCu != null)
                {
                    // Cập nhật đánh giá cũ
                    danhGiaCu.DiemDanhGia = model.DiemDanhGia;
                    danhGiaCu.NoiDungDanhGia = model.NoiDungDanhGia;
                    danhGiaCu.ThoiGianDanhGia = DateTime.Now;
                }
                else
                {
                    // Tạo đánh giá mới
                    var maDanhGia = await GenerateMaDanhGia();
                    var danhGiaMoi = new DanhGiaPhim
                    {
                        MaDanhGia = maDanhGia,
                        DiemDanhGia = model.DiemDanhGia,
                        NoiDungDanhGia = model.NoiDungDanhGia,
                        ThoiGianDanhGia = DateTime.Now,
                        MaKhachHang = maKhachHang!,
                        MaPhim = model.MaPhim
                    };

                    _context.DanhGiaPhims.Add(danhGiaMoi);
                }

                await _context.SaveChangesAsync();

                // Tính lại điểm trung bình
                var diemTrungBinh = await _context.DanhGiaPhims
                    .Where(d => d.MaPhim == model.MaPhim)
                    .AverageAsync(d => d.DiemDanhGia);

                var tongSoDanhGia = await _context.DanhGiaPhims
                    .CountAsync(d => d.MaPhim == model.MaPhim);

                return Json(new 
                { 
                    success = true, 
                    message = danhGiaCu != null ? "Cập nhật đánh giá thành công!" : "Đánh giá thành công!",
                    diemTrungBinh = Math.Round(diemTrungBinh, 1),
                    tongSoDanhGia = tongSoDanhGia
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPhimRating(string id)
        {
            var danhGias = await _context.DanhGiaPhims
                .Where(d => d.MaPhim == id)
                .ToListAsync();

            if (!danhGias.Any())
            {
                return Json(new { diemTrungBinh = 0, tongSoDanhGia = 0 });
            }

            var diemTrungBinh = danhGias.Average(d => d.DiemDanhGia);
            var tongSoDanhGia = danhGias.Count;

            return Json(new 
            { 
                diemTrungBinh = Math.Round(diemTrungBinh, 1), 
                tongSoDanhGia = tongSoDanhGia 
            });
        }

        private async Task<string> GenerateMaDanhGia()
        {
            var lastDanhGia = await _context.DanhGiaPhims
                .OrderByDescending(d => d.MaDanhGia)
                .FirstOrDefaultAsync();

            if (lastDanhGia == null)
                return "DG001";

            var lastNumber = int.Parse(lastDanhGia.MaDanhGia.Substring(2));
            return $"DG{(lastNumber + 1):D3}";
        }

        // Lấy danh sách phim đề xuất có lịch chiếu hôm nay
        private async Task<List<dynamic>> GetRecommendedMovies(Phim currentPhim, int count = 8)
        {
            var now = DateTime.Now;
            var endOfToday = DateTime.Today.AddDays(1);

            // Trim MaPhim để đảm bảo so sánh chính xác (loại bỏ khoảng trắng thừa)
            var currentPhimId = currentPhim.MaPhim?.Trim();

            // Lấy tất cả phim có lịch chiếu hôm nay từ thời điểm hiện tại trở đi
            var allMoviesWithTodaySchedule = await _context.LichChieus
                .Include(lc => lc.Phim)
                .Where(lc => lc.ThoiGianBatDau >= now && 
                           lc.ThoiGianBatDau < endOfToday)
                .Select(lc => lc.Phim)
                .Distinct()
                .ToListAsync();

            // Lọc ra phim hiện tại (client-side để tránh lỗi với Trim trong SQL)
            // Điều này đảm bảo phim đang xem không xuất hiện trong danh sách đề xuất
            var moviesWithTodaySchedule = allMoviesWithTodaySchedule
                .Where(p => p.MaPhim?.Trim() != currentPhimId)
                .ToList();

            if (!moviesWithTodaySchedule.Any())
            {
                // Nếu không có phim nào chiếu hôm nay, lấy phim có lịch chiếu trong 3 ngày tới
                var threeDaysLater = now.AddDays(3);
                var allMoviesInThreeDays = await _context.LichChieus
                    .Include(lc => lc.Phim)
                    .Where(lc => lc.ThoiGianBatDau >= now && 
                               lc.ThoiGianBatDau < threeDaysLater)
                    .Select(lc => lc.Phim)
                    .Distinct()
                    .ToListAsync();

                // Lọc ra phim hiện tại (client-side) - Fallback case
                moviesWithTodaySchedule = allMoviesInThreeDays
                    .Where(p => p.MaPhim?.Trim() != currentPhimId)
                    .Take(count)
                    .ToList();
            }

            var recommendedMovies = new List<dynamic>();

            foreach (var movie in moviesWithTodaySchedule.Take(count))
            {
                // Kiểm tra bổ sung lần cuối để đảm bảo 100% không đề xuất phim hiện tại
                // (Double-check cho trường hợp edge case)
                if (movie.MaPhim?.Trim() != currentPhimId)
                {
                    var movieData = await GetMovieWithStats(movie);
                    recommendedMovies.Add(movieData);
                }
            }

            // Sắp xếp theo độ ưu tiên:
            // 1. Phim cùng thể loại (+3 điểm)
            // 2. Phim có đánh giá cao >=7.0 (+2 điểm)  
            // 3. Phim phổ biến >=50 vé (+1 điểm)
            // 4. Sắp xếp theo đánh giá trung bình
            return recommendedMovies
                .OrderByDescending(m => ((Phim)m.Phim).TheLoai == currentPhim.TheLoai ? 3 : 0) 
                .ThenByDescending(m => (decimal)m.AvgRating >= 7.0m ? 2 : 0) 
                .ThenByDescending(m => (int)m.SoldTickets >= 50 ? 1 : 0) 
                .ThenByDescending(m => (decimal)m.AvgRating) 
                .Take(count)
                .ToList();
        }

        // Helper method để lấy thông tin phim kèm thống kê
        private async Task<dynamic> GetMovieWithStats(Phim phim)
        {
            var avgPrice = await _context.LichChieus
                .Where(lc => lc.MaPhim == phim.MaPhim)
                .AverageAsync(lc => (decimal?)lc.Gia) ?? 0;

            var avgRating = await _context.DanhGiaPhims
                .Where(dg => dg.MaPhim == phim.MaPhim)
                .AverageAsync(dg => (decimal?)dg.DiemDanhGia) ?? 0;

            var ratingCount = await _context.DanhGiaPhims
                .Where(dg => dg.MaPhim == phim.MaPhim)
                .CountAsync();

            var soldTickets = await _context.CTHDs
                .Include(ct => ct.Ve)
                .Where(ct => ct.Ve.MaPhim == phim.MaPhim)
                .CountAsync();

            var hasSchedule = await _context.LichChieus
                .AnyAsync(lc => lc.MaPhim == phim.MaPhim && lc.ThoiGianBatDau >= DateTime.Now);

            return new
            {
                Phim = phim,
                AvgPrice = avgPrice,
                AvgRating = avgRating,
                RatingCount = ratingCount,
                SoldTickets = soldTickets,
                HasSchedule = hasSchedule,
                RecommendReason = GetRecommendReason(phim, avgRating, soldTickets)
            };
        }

        // Xác định lý do đề xuất dựa trên lịch chiếu hôm nay
        private string GetRecommendReason(Phim phim, decimal avgRating, int soldTickets)
        {
            var now = DateTime.Now;
            var endOfToday = DateTime.Today.AddDays(1);
            
            // Kiểm tra xem phim có chiếu hôm nay từ bây giờ trở đi không
            var hasScheduleToday = _context.LichChieus
                .Any(lc => lc.MaPhim == phim.MaPhim && 
                          lc.ThoiGianBatDau >= now && 
                          lc.ThoiGianBatDau < endOfToday);
                          
            if (hasScheduleToday)
                return "Chiếu hôm nay";
            else if (avgRating >= 8.0m)
                return "Đánh giá cao";
            else if (soldTickets >= 50)
                return "Phổ biến";
            else if (avgRating >= 7.0m)
                return "Được yêu thích";
            else
                return "Sắp chiếu";
        }

        private bool VerifyPassword(string password, string hashedPassword)
        {
            // Kiểm tra nếu mật khẩu đã được hash
            if (hashedPassword.Length == 64) // SHA256 hash length
            {
                return HashPassword(password) == hashedPassword;
            }
            else
            {
                // Nếu là plain text (để test)
                return password == hashedPassword;
            }
        }

        // ============= SHOPPING FEATURES =============

        // Trang mua sắm sản phẩm
        public async Task<IActionResult> Shopping(
            string? searchTerm,
            string? danhMuc,
            decimal? giaMin,
            decimal? giaMax,
            string? sortBy = "name_asc")
        {
            if (!IsCustomerLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            var query = _context.SanPhams.Where(s => s.TrangThai == "Còn hàng" && s.SoLuongTon > 0);

            // Apply filters
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(s => s.TenSanPham.Contains(searchTerm) || (s.MoTa != null && s.MoTa.Contains(searchTerm)));
            }

            if (giaMin.HasValue)
            {
                query = query.Where(s => s.Gia >= giaMin.Value);
            }

            if (giaMax.HasValue)
            {
                query = query.Where(s => s.Gia <= giaMax.Value);
            }

            // Apply sorting
            query = sortBy switch
            {
                "price_asc" => query.OrderBy(s => s.Gia),
                "price_desc" => query.OrderByDescending(s => s.Gia),
                "name_desc" => query.OrderByDescending(s => s.TenSanPham),
                _ => query.OrderBy(s => s.TenSanPham)
            };

            var sanPhams = await query.ToListAsync();

            // Get featured and new products
            var sanPhamMoi = await _context.SanPhams
                .Where(s => s.TrangThai == "Còn hàng" && s.SoLuongTon > 0)
                .OrderByDescending(s => s.MaSanPham)
                .Take(6)
                .ToListAsync();

            var sanPhamBanChay = await _context.ChiTietHoaDonSanPhams
                .GroupBy(c => c.MaSanPham)
                .OrderByDescending(g => g.Sum(c => c.SoLuong))
                .Take(6)
                .Select(g => g.Key)
                .ToListAsync();

            var sanPhamBanChayList = await _context.SanPhams
                .Where(s => sanPhamBanChay.Contains(s.MaSanPham) && s.TrangThai == "Còn hàng")
                .ToListAsync();

            var viewModel = new ShoppingViewModel
            {
                SanPhams = sanPhams,
                SanPhamMoi = sanPhamMoi,
                SanPhamBanChay = sanPhamBanChayList,
                SearchTerm = searchTerm,
                DanhMuc = danhMuc,
                GiaMin = giaMin,
                GiaMax = giaMax,
                SortBy = sortBy
            };

            return View(viewModel);
        }

        // Chi tiết sản phẩm
        public async Task<IActionResult> ChiTietSanPham(string? id)
        {
            if (!IsCustomerLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            if (id == null)
            {
                return NotFound();
            }

            var sanPham = await _context.SanPhams
                .FirstOrDefaultAsync(s => s.MaSanPham == id);

            if (sanPham == null)
            {
                return NotFound();
            }

            // Get related products (by similar price range)
            var relatedProducts = await _context.SanPhams
                .Where(s => s.MaSanPham != id && 
                           s.TrangThai == "Còn hàng" &&
                           s.Gia >= sanPham.Gia * 0.7m && 
                           s.Gia <= sanPham.Gia * 1.3m)
                .Take(4)
                .ToListAsync();

            ViewBag.RelatedProducts = relatedProducts;
            return View(sanPham);
        }

        // Thêm vào giỏ hàng
        [HttpPost]
        public async Task<IActionResult> ThemVaoGioHang([FromBody] ThemVaoGioHangRequest request)
        {
            if (!IsCustomerLoggedIn())
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập" });
            }

            try
            {
                var maKhachHang = HttpContext.Session.GetString("MaKhachHang");
                var sanPham = await _context.SanPhams.FindAsync(request.MaSanPham);

                if (sanPham == null)
                {
                    return Json(new { success = false, message = "Sản phẩm không tồn tại" });
                }

                if (sanPham.SoLuongTon < request.SoLuong)
                {
                    return Json(new { success = false, message = "Không đủ số lượng trong kho" });
                }

                // Find or create cart
                var gioHang = await _context.GioHangs
                    .FirstOrDefaultAsync(g => g.MaKhachHang == maKhachHang && g.TrangThai == "Đang xử lý");

                if (gioHang == null)
                {
                    gioHang = new GioHang
                    {
                        MaGioHang = "GH" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                        MaKhachHang = maKhachHang!,
                        ThoiGianTao = DateTime.Now,
                        TrangThai = "Đang xử lý"
                    };
                    _context.GioHangs.Add(gioHang);
                    await _context.SaveChangesAsync();
                }

                // Check if product already in cart
                var chiTietGioHang = await _context.ChiTietGioHangs
                    .FirstOrDefaultAsync(c => c.MaGioHang == gioHang.MaGioHang && c.MaSanPham == request.MaSanPham);

                if (chiTietGioHang != null)
                {
                    chiTietGioHang.SoLuong += request.SoLuong;
                    _context.Update(chiTietGioHang);
                }
                else
                {
                    chiTietGioHang = new ChiTietGioHang
                    {
                        MaChiTietGioHang = "CTGH" + DateTime.Now.ToString("yyyyMMddHHmmss") + new Random().Next(1000, 9999),
                        MaGioHang = gioHang.MaGioHang,
                        MaSanPham = request.MaSanPham,
                        SoLuong = request.SoLuong,
                        DonGia = sanPham.Gia
                    };
                    _context.ChiTietGioHangs.Add(chiTietGioHang);
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đã thêm vào giỏ hàng" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // Xem giỏ hàng
        public async Task<IActionResult> GioHang()
        {
            if (!IsCustomerLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            var maKhachHang = HttpContext.Session.GetString("MaKhachHang");
            var gioHang = await _context.GioHangs
                .Include(g => g.ChiTietGioHangs)
                    .ThenInclude(c => c.SanPham)
                .FirstOrDefaultAsync(g => g.MaKhachHang == maKhachHang && g.TrangThai == "Đang xử lý");

            var viewModel = new GioHangViewModel();

            if (gioHang != null)
            {
                viewModel.GioHang = gioHang;
                viewModel.Items = gioHang.ChiTietGioHangs.Select(c => new GioHangItemViewModel
                {
                    ChiTiet = c,
                    SanPham = c.SanPham!
                }).ToList();

                viewModel.TongTien = viewModel.Items.Sum(i => i.ThanhTien);
                viewModel.TongSoLuong = viewModel.Items.Sum(i => i.ChiTiet.SoLuong);

                // Get applicable vouchers
                viewModel.VouchersApDung = await _context.VoucherSanPhams
                    .Where(v => v.ThoiGianBatDau <= DateTime.Now && 
                               v.ThoiGianKetThuc >= DateTime.Now && 
                               v.SoLuong > 0)
                    .ToListAsync();
            }

            return View(viewModel);
        }

        // Cập nhật giỏ hàng
        [HttpPost]
        public async Task<IActionResult> CapNhatGioHang([FromBody] CapNhatGioHangRequest request)
        {
            if (!IsCustomerLoggedIn())
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập" });
            }

            try
            {
                var chiTiet = await _context.ChiTietGioHangs
                    .Include(c => c.SanPham)
                    .FirstOrDefaultAsync(c => c.MaChiTietGioHang == request.MaChiTietGioHang);

                if (chiTiet == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ hàng" });
                }

                if (request.SoLuong <= 0)
                {
                    _context.ChiTietGioHangs.Remove(chiTiet);
                }
                else
                {
                    if (chiTiet.SanPham!.SoLuongTon < request.SoLuong)
                    {
                        return Json(new { success = false, message = "Không đủ số lượng trong kho" });
                    }

                    chiTiet.SoLuong = request.SoLuong;
                    _context.Update(chiTiet);
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Cập nhật giỏ hàng thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // Xóa khỏi giỏ hàng
        [HttpPost]
        public async Task<IActionResult> XoaKhoiGioHang(string maChiTietGioHang)
        {
            if (!IsCustomerLoggedIn())
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập" });
            }

            try
            {
                var chiTiet = await _context.ChiTietGioHangs.FindAsync(maChiTietGioHang);
                if (chiTiet != null)
                {
                    _context.ChiTietGioHangs.Remove(chiTiet);
                    await _context.SaveChangesAsync();
                }

                return Json(new { success = true, message = "Đã xóa sản phẩm khỏi giỏ hàng" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // Thanh toán sản phẩm
        public async Task<IActionResult> ThanhToanSanPham()
        {
            if (!IsCustomerLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            var maKhachHang = HttpContext.Session.GetString("MaKhachHang");
            var gioHang = await _context.GioHangs
                .Include(g => g.ChiTietGioHangs)
                    .ThenInclude(c => c.SanPham)
                .FirstOrDefaultAsync(g => g.MaKhachHang == maKhachHang && g.TrangThai == "Đang xử lý");

            if (gioHang == null || !gioHang.ChiTietGioHangs.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng trống!";
                return RedirectToAction(nameof(GioHang));
            }

            var khachHang = await _context.KhachHangs.FindAsync(maKhachHang);

            var gioHangViewModel = new GioHangViewModel
            {
                GioHang = gioHang,
                Items = gioHang.ChiTietGioHangs.Select(c => new GioHangItemViewModel
                {
                    ChiTiet = c,
                    SanPham = c.SanPham!
                }).ToList()
            };

            gioHangViewModel.TongTien = gioHangViewModel.Items.Sum(i => i.ThanhTien);
            gioHangViewModel.TongSoLuong = gioHangViewModel.Items.Sum(i => i.ChiTiet.SoLuong);

            var viewModel = new ThanhToanSanPhamViewModel
            {
                GioHang = gioHangViewModel,
                KhachHang = khachHang!,
                DiaChiGiaoHang = khachHang?.DiaChiGiaoHang ?? "",
                TongTienSauGiam = gioHangViewModel.TongTien
            };

            return View(viewModel);
        }

        // Xử lý thanh toán sản phẩm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThanhToanSanPham(ThanhToanSanPhamViewModel viewModel)
        {
            if (!IsCustomerLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            if (!ModelState.IsValid)
            {
                // Reload data for view
                await LoadThanhToanData(viewModel);
                return View(viewModel);
            }

            try
            {
                var maKhachHang = HttpContext.Session.GetString("MaKhachHang");
                var gioHang = await _context.GioHangs
                    .Include(g => g.ChiTietGioHangs)
                        .ThenInclude(c => c.SanPham)
                    .FirstOrDefaultAsync(g => g.MaKhachHang == maKhachHang && g.TrangThai == "Đang xử lý");

                if (gioHang == null || !gioHang.ChiTietGioHangs.Any())
                {
                    TempData["ErrorMessage"] = "Giỏ hàng trống!";
                    return RedirectToAction(nameof(GioHang));
                }

                // Create order
                var hoaDon = new HoaDonSanPham
                {
                    MaHoaDonSanPham = "HDSP" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                    MaKhachHang = maKhachHang!,
                    ThoiGianTao = DateTime.Now,
                    SoLuong = gioHang.ChiTietGioHangs.Sum(c => c.SoLuong),
                    TongTien = gioHang.ChiTietGioHangs.Sum(c => c.SoLuong * c.DonGia),
                    DiaChiGiaoHang = viewModel.DiaChiGiaoHang,
                    TrangThai = "Đang xử lý"
                };

                _context.HoaDonSanPhams.Add(hoaDon);

                // Create order details
                foreach (var item in gioHang.ChiTietGioHangs)
                {
                    var chiTiet = new ChiTietHoaDonSanPham
                    {
                        MaChiTietHoaDonSanPham = "CTHDSP" + DateTime.Now.ToString("yyyyMMddHHmmss") + new Random().Next(1000, 9999),
                        MaHoaDonSanPham = hoaDon.MaHoaDonSanPham,
                        MaSanPham = item.MaSanPham,
                        SoLuong = item.SoLuong,
                        DonGia = item.DonGia
                    };

                    _context.ChiTietHoaDonSanPhams.Add(chiTiet);

                    // Update product stock
                    item.SanPham!.SoLuongTon -= item.SoLuong;
                    if (item.SanPham.SoLuongTon <= 0)
                    {
                        item.SanPham.TrangThai = "Hết hàng";
                    }
                    _context.Update(item.SanPham);
                }

                // Update customer address
                var khachHang = await _context.KhachHangs.FindAsync(maKhachHang);
                if (khachHang != null)
                {
                    khachHang.DiaChiGiaoHang = viewModel.DiaChiGiaoHang;
                    _context.Update(khachHang);
                }

                // Clear cart
                gioHang.TrangThai = "Đã thanh toán";
                _context.Update(gioHang);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đặt hàng thành công!";
                return RedirectToAction(nameof(LichSuDonHang));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi đặt hàng: " + ex.Message;
                await LoadThanhToanData(viewModel);
                return View(viewModel);
            }
        }

        // Lịch sử đơn hàng
        public async Task<IActionResult> LichSuDonHang(
            string? trangThaiFilter,
            DateTime? tuNgay,
            DateTime? denNgay)
        {
            if (!IsCustomerLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            var maKhachHang = HttpContext.Session.GetString("MaKhachHang");
            var query = _context.HoaDonSanPhams
                .Include(h => h.ChiTietHoaDonSanPhams)
                    .ThenInclude(c => c.SanPham)
                .Where(h => h.MaKhachHang == maKhachHang);

            // Apply filters
            if (!string.IsNullOrEmpty(trangThaiFilter))
            {
                query = query.Where(h => h.TrangThai == trangThaiFilter);
            }

            if (tuNgay.HasValue)
            {
                query = query.Where(h => h.ThoiGianTao >= tuNgay.Value);
            }

            if (denNgay.HasValue)
            {
                query = query.Where(h => h.ThoiGianTao <= denNgay.Value.AddDays(1));
            }

            var donHangs = await query.OrderByDescending(h => h.ThoiGianTao).ToListAsync();

            var viewModel = new LichSuDonHangViewModel
            {
                DonHangs = donHangs.Select(h => new HoaDonSanPhamViewModel
                {
                    HoaDon = h,
                    Items = h.ChiTietHoaDonSanPhams.Select(c => new HoaDonSanPhamItemViewModel
                    {
                        ChiTiet = c,
                        SanPham = c.SanPham!
                    }).ToList(),
                    TongTienTruocGiam = h.ChiTietHoaDonSanPhams.Sum(c => c.SoLuong * c.DonGia)
                }).ToList(),
                TrangThaiFilter = trangThaiFilter,
                TuNgay = tuNgay,
                DenNgay = denNgay
            };

            return View(viewModel);
        }

        // Helper method to load checkout data
        private async Task LoadThanhToanData(ThanhToanSanPhamViewModel viewModel)
        {
            var maKhachHang = HttpContext.Session.GetString("MaKhachHang");
            var gioHang = await _context.GioHangs
                .Include(g => g.ChiTietGioHangs)
                    .ThenInclude(c => c.SanPham)
                .FirstOrDefaultAsync(g => g.MaKhachHang == maKhachHang && g.TrangThai == "Đang xử lý");

            var khachHang = await _context.KhachHangs.FindAsync(maKhachHang);

            if (gioHang != null)
            {
                viewModel.GioHang = new GioHangViewModel
                {
                    GioHang = gioHang,
                    Items = gioHang.ChiTietGioHangs.Select(c => new GioHangItemViewModel
                    {
                        ChiTiet = c,
                        SanPham = c.SanPham!
                    }).ToList()
                };

                viewModel.GioHang.TongTien = viewModel.GioHang.Items.Sum(i => i.ThanhTien);
                viewModel.GioHang.TongSoLuong = viewModel.GioHang.Items.Sum(i => i.ChiTiet.SoLuong);
            }

            viewModel.KhachHang = khachHang!;
            viewModel.TongTienSauGiam = viewModel.GioHang?.TongTien ?? 0;
        }



    }
}
