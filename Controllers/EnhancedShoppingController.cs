using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CinemaManagement.Data;
using CinemaManagement.Models;
using CinemaManagement.Services;
using CinemaManagement.ViewModels;
using CinemaManagement.Extensions;
using System.Text.Json;

namespace CinemaManagement.Controllers
{
    public class EnhancedShoppingController : Controller
    {
        private readonly CinemaDbContext _context;
        private readonly EnhancedShoppingService _enhancedShoppingService;
        private readonly ILogger<EnhancedShoppingController> _logger;

        public EnhancedShoppingController(
            CinemaDbContext context,
            EnhancedShoppingService enhancedShoppingService,
            ILogger<EnhancedShoppingController> logger)
        {
            _context = context;
            _enhancedShoppingService = enhancedShoppingService;
            _logger = logger;
        }

        private bool IsCustomerLoggedIn()
        {
            return HttpContext.Session.GetString("Role") == "Khách hàng" && 
                   !string.IsNullOrEmpty(HttpContext.Session.GetString("MaKhachHang"));
        }

        #region Địa chỉ giao hàng



        [HttpPost]
        public async Task<IActionResult> AddAddress(AddAddressRequest request)
        {
            if (!IsCustomerLoggedIn())
                return Json(new { success = false, message = "Vui lòng đăng nhập" });

            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Dữ liệu không hợp lệ" });

            try
            {
                var maKhachHang = HttpContext.Session.GetString("MaKhachHang");

                var newAddress = new DiaChiInfo
                {
                    TenNguoiNhan = request.TenNguoiNhan,
                    SoDienThoai = request.SoDienThoai,
                    DiaChiChiTiet = request.DiaChiChiTiet,
                    PhuongXa = request.PhuongXa,
                    QuanHuyen = request.QuanHuyen,
                    TinhThanh = request.TinhThanh,
                    LaMacDinh = request.LaMacDinh
                };

                var success = await _enhancedShoppingService.AddCustomerAddressAsync(maKhachHang!, newAddress);

                if (success)
                    return Json(new { success = true, message = "Thêm địa chỉ thành công" });
                else
                    return Json(new { success = false, message = "Có lỗi xảy ra khi thêm địa chỉ" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding address for customer");
                return Json(new { success = false, message = "Có lỗi xảy ra" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAddress(string maDiaChi)
        {
            if (!IsCustomerLoggedIn())
                return Json(new { success = false, message = "Vui lòng đăng nhập" });

            try
            {
                var maKhachHang = HttpContext.Session.GetString("MaKhachHang");
                
                var success = await _enhancedShoppingService.DeleteCustomerAddressAsync(maKhachHang!, maDiaChi);

                if (success)
                    return Json(new { success = true, message = "Xóa địa chỉ thành công" });
                else
                    return Json(new { success = false, message = "Không tìm thấy địa chỉ hoặc có lỗi xảy ra" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting address {MaDiaChi}", maDiaChi);
                return Json(new { success = false, message = "Có lỗi xảy ra" });
            }
        }

        #endregion

        #region Enhanced Checkout

        [HttpGet]
        public async Task<IActionResult> EnhancedCheckout()
        {
            if (!IsCustomerLoggedIn())
                return RedirectToAction("Login", "Auth");

            try
            {
                var maKhachHang = HttpContext.Session.GetString("MaKhachHang");

                // Lấy giỏ hàng từ database
                var gioHang = await _context.GioHangs
                    .Include(g => g.ChiTietGioHangs)
                        .ThenInclude(c => c.SanPham)
                    .FirstOrDefaultAsync(g => g.MaKhachHang == maKhachHang && g.TrangThai == "Đang xử lý");

                if (gioHang == null || !gioHang.ChiTietGioHangs.Any())
                {
                    TempData["ErrorMessage"] = "Giỏ hàng trống!";
                    return RedirectToAction("GioHang", "KhachHang");
                }

                // Tạo ViewModel
                var viewModel = new EnhancedCheckoutViewModel();

                // Thông tin khách hàng
                viewModel.KhachHang = await _context.KhachHangs
                    .FirstOrDefaultAsync(k => k.MaKhachHang == maKhachHang) ?? new KhachHang();

                // Giỏ hàng
                viewModel.GioHang = new GioHangViewModel
                {
                    GioHang = gioHang,
                    Items = gioHang.ChiTietGioHangs.Select(c => new GioHangItemViewModel
                    {
                        ChiTiet = c,
                        SanPham = c.SanPham!
                    }).ToList(),
                    TongTien = gioHang.ChiTietGioHangs.Sum(c => c.SoLuong * c.DonGia),
                    TongSoLuong = gioHang.ChiTietGioHangs.Sum(c => c.SoLuong)
                };

                // Địa chỉ giao hàng từ tài khoản
                var khachHang = await _context.KhachHangs.FirstOrDefaultAsync(k => k.MaKhachHang == maKhachHang);
                viewModel.DiaChiTaiKhoan = khachHang?.DiaChiGiaoHang;

                // Phương thức vận chuyển
                viewModel.DanhSachPhuongThucVC = _enhancedShoppingService.GetAvailableShippingMethods();

                // Phương thức thanh toán
                viewModel.DanhSachPhuongThucTT = _enhancedShoppingService.GetAvailablePaymentMethods(viewModel.GioHang.TongTien);

                // Voucher
                viewModel.VouchersApDung = await _context.VoucherSanPhams
                    .Where(v => v.ThoiGianBatDau <= DateTime.Now && 
                               v.ThoiGianKetThuc >= DateTime.Now && 
                               v.SoLuong > 0)
                    .ToListAsync();

                // Tính toán phí
                viewModel.TongTienHang = viewModel.GioHang.TongTien;

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading enhanced checkout");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải trang thanh toán";
                return RedirectToAction("GioHang", "KhachHang");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CalculateShipping(CalculateShippingRequest request)
        {
            if (!IsCustomerLoggedIn())
                return Json(new { success = false, message = "Vui lòng đăng nhập" });

            try
            {
                var phiVanChuyen = _enhancedShoppingService.CalculateShippingFee(
                    request.TinhThanh, "STANDARD", request.TongTienHang);

                bool mienPhiVanChuyen = request.TongTienHang >= ShoppingConfiguration.DON_HANG_TOI_THIEU_MIEN_PHI_SHIP;
                decimal soTienConLai = Math.Max(0, ShoppingConfiguration.DON_HANG_TOI_THIEU_MIEN_PHI_SHIP - request.TongTienHang);

                return Json(new
                {
                    success = true,
                    phiVanChuyen = mienPhiVanChuyen ? 0 : phiVanChuyen,
                    mienPhiVanChuyen = mienPhiVanChuyen,
                    soTienConLaiDeMienPhi = soTienConLai
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating shipping fee");
                return Json(new { success = false, message = "Có lỗi xảy ra khi tính phí vận chuyển" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> PlaceEnhancedOrder(EnhancedCheckoutRequest request)
        {
            if (!IsCustomerLoggedIn())
                return Json(new { success = false, message = "Vui lòng đăng nhập" });

            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Dữ liệu không hợp lệ" });

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var maKhachHang = HttpContext.Session.GetString("MaKhachHang");

                // Lấy giỏ hàng
                var gioHang = await _context.GioHangs
                    .Include(g => g.ChiTietGioHangs)
                        .ThenInclude(c => c.SanPham)
                    .FirstOrDefaultAsync(g => g.MaKhachHang == maKhachHang && g.TrangThai == "Đang xử lý");

                if (gioHang == null || !gioHang.ChiTietGioHangs.Any())
                    return Json(new { success = false, message = "Giỏ hàng trống" });

                // Tính toán các khoản phí
                var tongTienHang = gioHang.ChiTietGioHangs.Sum(c => c.SoLuong * c.DonGia);
                
                // Lấy thông tin địa chỉ giao hàng
                var khachHang = await _context.KhachHangs.FirstOrDefaultAsync(k => k.MaKhachHang == maKhachHang);
                string diaChiGiaoHang = "";
                
                if (request.SuDungDiaChiTaiKhoan)
                {
                    diaChiGiaoHang = khachHang?.DiaChiGiaoHang ?? "";
                }
                else
                {
                    diaChiGiaoHang = request.DiaChiGiaoHangMoi ?? "";
                }

                var phiVanChuyen = _enhancedShoppingService.CalculateShippingFee(
                    "Hà Nội", request.PhuongThucVanChuyen, tongTienHang); // Simplified - không phân tích địa chỉ

                // Áp dụng voucher nếu có
                decimal tienGiam = 0;
                if (!string.IsNullOrEmpty(request.MaVoucher))
                {
                    var voucher = await _context.VoucherSanPhams
                        .FirstOrDefaultAsync(v => v.MaVoucherSanPham == request.MaVoucher);

                    if (voucher != null && voucher.SoLuong > 0)
                    {
                        tienGiam = Math.Min(
                            tongTienHang * voucher.PhanTramGiam / 100,
                            voucher.GiaTriGiamToiDa);
                    }
                }

                var tongTienSauGiam = tongTienHang + phiVanChuyen - tienGiam;

                // Xử lý thanh toán
                var paymentResult = await _enhancedShoppingService.ProcessPaymentAsync(
                    request.PhuongThucThanhToan, tongTienSauGiam, request.ThongTinThanhToan);
                
                var paymentSuccess = paymentResult.Success;
                var paymentMessage = paymentResult.Message;
                var transactionId = paymentResult.TransactionId;

                if (!paymentSuccess)
                    return Json(new { success = false, message = paymentMessage });

                // Tạo hóa đơn
                var hoaDon = new HoaDonSanPham
                {
                    MaHoaDonSanPham = "HDSP" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                    MaKhachHang = maKhachHang!,
                    ThoiGianTao = DateTime.Now,
                    SoLuong = gioHang.ChiTietGioHangs.Sum(c => c.SoLuong),
                    TongTien = tongTienSauGiam,
                    TrangThai = "Đã đặt",
                    DiaChiGiaoHang = diaChiGiaoHang
                };

                _context.HoaDonSanPhams.Add(hoaDon);

                // Tạo chi tiết hóa đơn
                foreach (var item in gioHang.ChiTietGioHangs)
                {
                    var chiTiet = new ChiTietHoaDonSanPham
                    {
                        MaChiTietHoaDonSanPham = "CT" + DateTime.Now.ToString("yyyyMMddHHmmss") + item.MaSanPham,
                        MaHoaDonSanPham = hoaDon.MaHoaDonSanPham,
                        MaSanPham = item.MaSanPham,
                        SoLuong = item.SoLuong,
                        DonGia = item.DonGia
                    };
                    _context.ChiTietHoaDonSanPhams.Add(chiTiet);

                    // Cập nhật tồn kho
                    if (item.SanPham != null)
                    {
                        item.SanPham.SoLuongTon -= item.SoLuong;
                        _context.Update(item.SanPham);
                    }
                }

                // Cập nhật voucher
                if (!string.IsNullOrEmpty(request.MaVoucher) && tienGiam > 0)
                {
                    var voucher = await _context.VoucherSanPhams
                        .FirstOrDefaultAsync(v => v.MaVoucherSanPham == request.MaVoucher);
                    if (voucher != null)
                    {
                        voucher.SoLuong -= 1;
                        _context.Update(voucher);

                        // Lưu thông tin voucher đã sử dụng
                        var hoaDonVoucher = new HoaDonSanPhamVoucher
                        {
                            MaHoaDonSanPham = hoaDon.MaHoaDonSanPham,
                            MaVoucherSanPham = request.MaVoucher,
                            SoLuongVoucher = 1,
                            TongTienGiam = tienGiam
                        };
                        _context.HoaDonSanPhamVouchers.Add(hoaDonVoucher);
                    }
                }

                // Tạo trạng thái đơn hàng đầu tiên
                await _enhancedShoppingService.CreateOrderTrackingAsync(
                    hoaDon.MaHoaDonSanPham, "Đã đặt", "Đơn hàng đã được đặt thành công");

                // Xóa giỏ hàng
                _context.ChiTietGioHangs.RemoveRange(gioHang.ChiTietGioHangs);
                _context.GioHangs.Remove(gioHang);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new
                {
                    success = true,
                    message = paymentMessage,
                    orderId = hoaDon.MaHoaDonSanPham,
                    transactionId = transactionId
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error placing enhanced order");
                return Json(new { success = false, message = "Có lỗi xảy ra khi đặt hàng" });
            }
        }

        #endregion

        #region Order Tracking

        [HttpGet]
        public async Task<IActionResult> TrackOrder(string? orderId)
        {
            if (!IsCustomerLoggedIn())
                return RedirectToAction("Login", "Auth");

            if (string.IsNullOrEmpty(orderId))
                return View("TrackOrderSearch");

            try
            {
                var maKhachHang = HttpContext.Session.GetString("MaKhachHang");

                var hoaDon = await _context.HoaDonSanPhams
                    .Include(h => h.ChiTietHoaDonSanPhams)
                        .ThenInclude(c => c.SanPham)
                    .FirstOrDefaultAsync(h => h.MaHoaDonSanPham == orderId && h.MaKhachHang == maKhachHang);

                if (hoaDon == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn hàng";
                    return View("TrackOrderSearch");
                }

                var lichSuTrangThai = _enhancedShoppingService.GetOrderTrackingHistory(hoaDon);

                var viewModel = new TrackingOrderViewModel
                {
                    HoaDon = hoaDon,
                    LichSuTrangThai = lichSuTrangThai,
                    ChiTietDonHang = hoaDon.ChiTietHoaDonSanPhams.ToList(),
                    TrangThaiHienTai = hoaDon.TrangThai,
                    ThoiGianCapNhatCuoi = lichSuTrangThai.LastOrDefault()?.ThoiGianCapNhat,
                    ThoiGianGiaoHangDuKien = _enhancedShoppingService.CalculateExpectedDeliveryTime("STANDARD")
                };

                if (lichSuTrangThai.Any())
                {
                    var trangThaiCuoi = lichSuTrangThai.Last();
                    viewModel.ViTriHienTai = trangThaiCuoi.ViTri;
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking order {OrderId}", orderId);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tra cứu đơn hàng";
                return View("TrackOrderSearch");
            }
        }

        #endregion

        #region Order History & Reorder

        [HttpGet]
        public async Task<IActionResult> OrderHistory(string? status, DateTime? fromDate, DateTime? toDate, string? search, int page = 1)
        {
            if (!IsCustomerLoggedIn())
                return RedirectToAction("Login", "Auth");

            try
            {
                var maKhachHang = HttpContext.Session.GetString("MaKhachHang");

                // Simplified order history - lấy từ database trực tiếp
                var orders = await _context.HoaDonSanPhams
                    .Include(h => h.ChiTietHoaDonSanPhams)
                        .ThenInclude(c => c.SanPham)
                    .Where(h => h.MaKhachHang == maKhachHang)
                    .OrderByDescending(h => h.ThoiGianTao)
                    .Take(50) // Giới hạn 50 đơn hàng gần nhất
                    .ToListAsync();

                var orderViewModels = orders.Select(h => new HoaDonSanPhamDetailViewModel
                {
                    HoaDon = h,
                    ChiTiet = h.ChiTietHoaDonSanPhams.ToList(),
                    LichSuTrangThai = _enhancedShoppingService.GetOrderTrackingHistory(h),
                    TongTienTruocGiam = h.ChiTietHoaDonSanPhams.Sum(c => c.SoLuong * c.DonGia),
                    TienGiam = 0,
                    CoTheMuaLai = h.TrangThai == "Đã giao",
                    CoTheHuy = h.TrangThai == "Đang xử lý",
                    CoTheTraHang = h.TrangThai == "Đã giao" && h.ThoiGianTao >= DateTime.Now.AddDays(-7)
                }).ToList();

                var viewModel = new EnhancedOrderHistoryViewModel
                {
                    DonHangs = orderViewModels,
                    TongDonHang = orders.Count,
                    TongChiTieu = orders.Sum(o => o.TongTien),
                    DonHangHoanThanh = orders.Count(o => o.TrangThai == "Đã giao"),
                    DonHangDangXuLy = orders.Count(o => o.TrangThai == "Đang xử lý")
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading order history");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải lịch sử đơn hàng";
                return View(new EnhancedOrderHistoryViewModel());
            }
        }

        [HttpGet]
        public async Task<IActionResult> PrepareReorder(string orderId)
        {
            if (!IsCustomerLoggedIn())
                return RedirectToAction("Login", "Auth");

            try
            {
                var maKhachHang = HttpContext.Session.GetString("MaKhachHang");

                var reorderData = await _enhancedShoppingService.PrepareReorderAsync(maKhachHang!, orderId);

                if (reorderData == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn hàng để mua lại";
                    return RedirectToAction("OrderHistory");
                }

                return View(reorderData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error preparing reorder for {OrderId}", orderId);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi chuẩn bị mua lại";
                return RedirectToAction("OrderHistory");
            }
        }

        [HttpPost]
        public IActionResult ExecuteReorder(ReorderViewModel model)
        {
            if (!IsCustomerLoggedIn())
                return Json(new { success = false, message = "Vui lòng đăng nhập" });

            try
            {
                var maKhachHang = HttpContext.Session.GetString("MaKhachHang");

                // Simplified reorder - chuyển các items được chọn vào giỏ hàng
                var cartItems = new List<object>();
                
                foreach (var item in model.Items.Where(i => i.ChonMua && i.ConHang))
                {
                    cartItems.Add(new
                    {
                        MaSanPham = item.ChiTietGoc.MaSanPham,
                        TenSanPham = item.SanPhamHienTai?.TenSanPham ?? "",
                        SoLuong = item.SoLuongMua,
                        DonGia = item.GiaHienTai,
                        HinhAnh = item.SanPhamHienTai?.HinhAnh
                    });
                }

                if (cartItems.Any())
                {
                    HttpContext.Session.SetObjectAsJson("TempGioHang", cartItems);
                    return Json(new { success = true, message = $"Đã thêm {cartItems.Count} sản phẩm vào giỏ hàng" });
                }
                else
                {
                    return Json(new { success = false, message = "Không có sản phẩm nào có thể thêm vào giỏ hàng" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing reorder");
                return Json(new { success = false, message = "Có lỗi xảy ra khi thực hiện mua lại" });
            }
        }

        #endregion
    }
} 