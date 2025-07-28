using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CinemaManagement.Data;
using CinemaManagement.Models;
using CinemaManagement.Services;
using CinemaManagement.Extensions;
using CinemaManagement.ViewModels;
using Net.payOS.Types;
using System.Text.Json;

namespace CinemaManagement.Controllers
{
    public class PaymentController : Controller
    {
        private readonly PayOSService _payOSService;
        private readonly CinemaDbContext _context;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(PayOSService payOSService, CinemaDbContext context, ILogger<PaymentController> logger)
        {
            _payOSService = payOSService;
            _context = context;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreatePaymentLink(string? maVoucher)
        {
            try
            {
                _logger.LogInformation("Creating PayOS payment link");

                // Kiểm tra đăng nhập
                var role = HttpContext.Session.GetString("Role");
                var maKhachHang = HttpContext.Session.GetString("MaKhachHang");
                if (role != "Khách hàng" || string.IsNullOrEmpty(maKhachHang))
                {
                    return RedirectToAction("Login", "Auth");
                }

                // Lấy giỏ hàng từ session
                var gioHang = HttpContext.Session.GetObjectFromJson<List<GioHangItem>>("TempGioHang") ?? new List<GioHangItem>();
                if (!gioHang.Any())
                {
                    TempData["ErrorMessage"] = "Giỏ hàng trống";
                    return RedirectToAction("Index", "KhachHang");
                }

                // Tính tổng tiền
                decimal tongTienGoc = gioHang.Sum(g => g.Gia);
                decimal giaGiam = 0;

                // Xử lý voucher
                if (!string.IsNullOrEmpty(maVoucher))
                {
                    var voucher = await _context.Vouchers.FirstOrDefaultAsync(v => v.MaGiamGia == maVoucher);
                    if (voucher != null && voucher.ThoiGianBatDau <= DateTime.Now && voucher.ThoiGianKetThuc >= DateTime.Now)
                    {
                        giaGiam = tongTienGoc * voucher.PhanTramGiam / 100;
                    }
                }

                decimal tongTienSauGiam = tongTienGoc - giaGiam;

                // Tạo order code duy nhất (PayOS yêu cầu số nguyên dương nhỏ hơn)
                var random = new Random();
                int orderCode = random.Next(1000000, 9999999); // Tạo số từ 1,000,000 đến 9,999,999

                // Lưu thông tin đơn hàng vào session để xử lý sau
                var orderInfo = new
                {
                    OrderCode = orderCode,
                    MaKhachHang = maKhachHang,
                    GioHang = gioHang,
                    MaVoucher = maVoucher,
                    TongTienGoc = tongTienGoc,
                    GiaGiam = giaGiam,
                    TongTienSauGiam = tongTienSauGiam,
                    CreatedAt = DateTime.Now
                };

                HttpContext.Session.SetString($"order_{orderCode}", JsonSerializer.Serialize(orderInfo));

                // Tạo PaymentData theo đúng hướng dẫn PayOS
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var paymentData = new PaymentData(
                    orderCode: orderCode,
                    amount: (int)tongTienSauGiam,
                    description: "Thanh toan ve phim",
                    items: gioHang.Select(g => new ItemData(
                        name: $"Ve {g.TenPhim.Substring(0, Math.Min(g.TenPhim.Length, 15))}",
                        quantity: 1,
                        price: (int)g.Gia
                    )).ToList(),
                    returnUrl: $"{baseUrl}/Payment/PaymentReturn?orderCode={orderCode}",
                    cancelUrl: $"{baseUrl}/Payment/PaymentCancel?orderCode={orderCode}"
                );

                // Tạo payment link từ PayOS
                var paymentResponse = await _payOSService.CreatePaymentLink(paymentData);

                if (paymentResponse != null && !string.IsNullOrEmpty(paymentResponse.checkoutUrl))
                {
                    _logger.LogInformation("Payment link created successfully for order {OrderCode}: {CheckoutUrl}", 
                        orderCode, paymentResponse.checkoutUrl);
                    
                    // Chuyển hướng đến trang thanh toán PayOS
                    return Redirect(paymentResponse.checkoutUrl);
                }
                else
                {
                    _logger.LogError("Failed to create PayOS payment link for order: {OrderCode}", orderCode);
                    TempData["ErrorMessage"] = "Không thể tạo liên kết thanh toán PayOS. Vui lòng thử lại.";
                    return RedirectToAction("ThanhToan", "KhachHang");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment link");
                TempData["ErrorMessage"] = $"Lỗi hệ thống: {ex.Message}";
                return RedirectToAction("ThanhToan", "KhachHang");
            }
        }

        public async Task<IActionResult> PaymentReturn(int orderCode)
        {
            try
            {
                _logger.LogInformation("Processing payment return for order: {OrderCode}", orderCode);

                // Lấy thông tin đơn hàng từ session
                var orderInfoJson = HttpContext.Session.GetString($"order_{orderCode}");
                if (string.IsNullOrEmpty(orderInfoJson))
                {
                    _logger.LogWarning("Order information not found in session for order: {OrderCode}", orderCode);
                    TempData["ErrorMessage"] = "Không tìm thấy thông tin đơn hàng. Vui lòng thử đặt vé lại.";
                    return RedirectToAction("Index", "KhachHang");
                }

                var orderInfo = JsonSerializer.Deserialize<JsonElement>(orderInfoJson);

                // Kiểm tra trạng thái thanh toán từ PayOS
                var paymentInfo = await _payOSService.GetPaymentLinkInformation(orderCode);

                if (paymentInfo != null && paymentInfo.status == "PAID")
                {
                    _logger.LogInformation("Payment successful for order: {OrderCode}", orderCode);
                    
                    // Thanh toán thành công - Lưu hóa đơn
                    var maHoaDon = await ProcessSuccessfulPayment(orderInfo, paymentInfo);

                    // Xóa thông tin đơn hàng khỏi session sau khi xử lý thành công
                    HttpContext.Session.Remove($"order_{orderCode}");
                    HttpContext.Session.Remove("TempGioHang");

                    _logger.LogInformation("Payment processed successfully. Invoice created: {MaHoaDon}", maHoaDon);
                    TempData["SuccessMessage"] = "Thanh toán thành công! Hóa đơn đã được tạo.";
                    return RedirectToAction("ThanhToanThanhCong", "KhachHang", new { maHoaDon = maHoaDon });
                }
                else if (paymentInfo != null && paymentInfo.status == "CANCELLED")
                {
                    _logger.LogInformation("Payment was cancelled by user for order: {OrderCode}", orderCode);
                    
                    // Khôi phục giỏ hàng
                    RestoreShoppingCart(orderInfo);
                    
                    // Xóa thông tin đơn hàng khỏi session
                    HttpContext.Session.Remove($"order_{orderCode}");
                    
                    TempData["ErrorMessage"] = "Thanh toán đã bị hủy. Giỏ hàng đã được khôi phục, bạn có thể thử thanh toán lại.";
                    return RedirectToAction("ThanhToan", "KhachHang");
                }
                else if (paymentInfo != null && paymentInfo.status == "PENDING")
                {
                    _logger.LogInformation("Payment is still pending for order: {OrderCode}", orderCode);
                    TempData["InfoMessage"] = "Thanh toán đang được xử lý. Vui lòng đợi trong giây lát hoặc kiểm tra lại sau.";
                    return RedirectToAction("ThanhToan", "KhachHang");
                }
                else
                {
                    _logger.LogWarning("Payment failed or status unknown for order: {OrderCode}. Status: {Status}", 
                        orderCode, paymentInfo?.status ?? "null");
                        
                    // Khôi phục giỏ hàng
                    RestoreShoppingCart(orderInfo);
                    
                    TempData["ErrorMessage"] = "Thanh toán thất bại hoặc chưa được xác nhận. Giỏ hàng đã được khôi phục, vui lòng thử lại.";
                    return RedirectToAction("PaymentFailed", new { orderCode = orderCode });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment return for order: {OrderCode}", orderCode);
                TempData["ErrorMessage"] = $"Lỗi xử lý thanh toán: {ex.Message}";
                return RedirectToAction("PaymentFailed", new { orderCode = orderCode });
            }
        }

        public IActionResult PaymentCancel(int orderCode)
        {
            _logger.LogInformation("Payment cancelled for order: {OrderCode}", orderCode);

            // Lấy thông tin đơn hàng từ session để khôi phục giỏ hàng
            var orderInfoJson = HttpContext.Session.GetString($"order_{orderCode}");
            if (!string.IsNullOrEmpty(orderInfoJson))
            {
                try
                {
                    var orderInfo = JsonSerializer.Deserialize<JsonElement>(orderInfoJson);
                    RestoreShoppingCart(orderInfo);
                    _logger.LogInformation("Shopping cart restored for cancelled order: {OrderCode}", orderCode);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not restore shopping cart for cancelled order: {OrderCode}", orderCode);
                }
            }

            // Xóa thông tin đơn hàng khỏi session
            HttpContext.Session.Remove($"order_{orderCode}");

            TempData["ErrorMessage"] = "Thanh toán đã bị hủy bởi người dùng. Giỏ hàng đã được khôi phục, bạn có thể thử thanh toán lại.";
            return RedirectToAction("ThanhToan", "KhachHang");
        }

        public IActionResult PaymentFailed(int orderCode)
        {
            _logger.LogInformation("Payment failed for order: {OrderCode}", orderCode);

            // Lấy thông tin đơn hàng từ session để khôi phục giỏ hàng
            var orderInfoJson = HttpContext.Session.GetString($"order_{orderCode}");
            if (!string.IsNullOrEmpty(orderInfoJson))
            {
                try
                {
                    var orderInfo = JsonSerializer.Deserialize<JsonElement>(orderInfoJson);
                    RestoreShoppingCart(orderInfo);
                    _logger.LogInformation("Shopping cart restored for failed order: {OrderCode}", orderCode);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not restore shopping cart for failed order: {OrderCode}", orderCode);
                }
            }

            // Lưu lại orderCode để hiển thị trên trang lỗi
            ViewBag.OrderCode = orderCode;
            
            // Không xóa thông tin đơn hàng ngay để user có thể retry
            if (TempData["ErrorMessage"] == null)
            {
                TempData["ErrorMessage"] = "Thanh toán thất bại. Giỏ hàng đã được khôi phục, vui lòng thử lại.";
            }
            
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> PaymentWebhook()
        {
            try
            {
                var webhookBody = await new StreamReader(Request.Body).ReadToEndAsync();
                var signature = Request.Headers["X-PAYOS-Signature"].FirstOrDefault();

                if (string.IsNullOrEmpty(signature))
                {
                    return BadRequest("Missing signature");
                }

                var webhookData = _payOSService.VerifyPaymentWebhookData(webhookBody, signature);

                if (webhookData != null)
                {
                    _logger.LogInformation("Valid webhook received for order: {OrderCode}", webhookData.orderCode);
                    return Ok(new { message = "Webhook processed successfully" });
                }
                else
                {
                    return BadRequest(new { message = "Invalid webhook data" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        private void RestoreShoppingCart(JsonElement orderInfo)
        {
            try
            {
                var gioHangJson = orderInfo.GetProperty("GioHang").GetRawText();
                HttpContext.Session.SetString("TempGioHang", gioHangJson);
                _logger.LogInformation("Shopping cart restored from order information");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not restore shopping cart from order information");
            }
        }

        private async Task<string> ProcessSuccessfulPayment(JsonElement orderInfo, PaymentLinkInformation paymentInfo)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var maKhachHang = orderInfo.GetProperty("MaKhachHang").GetString();
                if (string.IsNullOrEmpty(maKhachHang)) 
                    throw new Exception("Không tìm thấy mã khách hàng");
                    
                var gioHangJson = orderInfo.GetProperty("GioHang").GetRawText();
                var gioHang = JsonSerializer.Deserialize<List<GioHangItem>>(gioHangJson);
                var maVoucher = orderInfo.TryGetProperty("MaVoucher", out var voucherProp) && !voucherProp.ValueKind.Equals(JsonValueKind.Null) 
                    ? voucherProp.GetString() : null;
                var tongTienSauGiam = orderInfo.GetProperty("TongTienSauGiam").GetDecimal();
                var giaGiam = orderInfo.GetProperty("GiaGiam").GetDecimal();

                var khachHang = await _context.KhachHangs.FindAsync(maKhachHang);
                if (khachHang == null) throw new Exception("Không tìm thấy khách hàng");

                // Tạo hóa đơn
                var maHoaDon = $"HD{DateTime.Now:yyyyMMddHHmmss}";
                var hoaDon = new HoaDon
                {
                    MaHoaDon = maHoaDon,
                    MaKhachHang = maKhachHang,
                    MaNhanVien = "GUEST",
                    ThoiGianTao = DateTime.Now,
                    TongTien = tongTienSauGiam,
                    SoLuong = gioHang?.Count ?? 0
                };

                _context.HoaDons.Add(hoaDon);

                // Tạo vé và chi tiết hóa đơn
                var diemTichLuyNhan = 0;
               if (gioHang != null)
                {
                    foreach (var item in gioHang)
                    {
                        // Tìm vé đã tồn tại
                        var ve = await _context.Ves
                            .FirstOrDefaultAsync(v => v.MaGhe == item.MaGhe && v.MaLichChieu == item.MaLichChieu);

                        if (ve == null)
                            throw new Exception($"Không tìm thấy vé cho ghế {item.SoGhe} lịch chiếu {item.MaLichChieu}");

                        // Cập nhật trạng thái vé
                        ve.TrangThai = "Đã bán";
                        ve.Gia = item.Gia; // Nếu cần cập nhật giá

                        // Tạo chi tiết hóa đơn
                        var chiTiet = new CTHD
                        {
                            MaCTHD = $"CT{DateTime.Now:yyyyMMddHHmmss}{item.SoGhe}",
                            MaHoaDon = maHoaDon,
                            MaVe = ve.MaVe,
                            DonGia = item.Gia
                        };
                        _context.CTHDs.Add(chiTiet);

                        // Tính điểm tích lũy
                        diemTichLuyNhan += (int)(item.Gia / 1000);
                    }
                }

                // Cập nhật điểm tích lũy
                khachHang.DiemTichLuy += diemTichLuyNhan;

                // Xử lý voucher nếu có
                if (!string.IsNullOrEmpty(maVoucher))
                {
                    var voucher = await _context.Vouchers.FindAsync(maVoucher);
                    if (voucher != null)
                    {
                        var hdVoucher = new HDVoucher
                        {
                            MaHoaDon = maHoaDon,
                            MaGiamGia = maVoucher,
                            SoLuongVoucher = 1,
                            TongTien = giaGiam
                        };
                        _context.HDVouchers.Add(hdVoucher);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Payment processed successfully. Invoice: {MaHoaDon}", maHoaDon);

                return maHoaDon;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error processing successful payment");
                throw;
            }
        }
    }
}
