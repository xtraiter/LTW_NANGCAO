using CinemaManagement.Data;
using CinemaManagement.Models;
using CinemaManagement.ViewModels;
using CinemaManagement.Extensions;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CinemaManagement.Services
{
    public class EnhancedShoppingService
    {
        private readonly CinemaDbContext _context;
        private readonly ILogger<EnhancedShoppingService> _logger;

        public EnhancedShoppingService(CinemaDbContext context, ILogger<EnhancedShoppingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Địa chỉ giao hàng

        /// <summary>
        /// Lấy danh sách địa chỉ đã lưu của khách hàng từ trường DiaChiGiaoHang (JSON)
        /// </summary>
        public async Task<List<DiaChiInfo>> GetCustomerAddressesAsync(string maKhachHang)
        {
            try
            {
                var khachHang = await _context.KhachHangs.FirstOrDefaultAsync(k => k.MaKhachHang == maKhachHang);
                if (khachHang?.DiaChiGiaoHang == null)
                    return new List<DiaChiInfo>();

                var addresses = JsonSerializer.Deserialize<List<DiaChiInfo>>(khachHang.DiaChiGiaoHang);
                return addresses ?? new List<DiaChiInfo>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting addresses for customer {MaKhachHang}", maKhachHang);
                return new List<DiaChiInfo>();
            }
        }

        /// <summary>
        /// Thêm địa chỉ mới cho khách hàng
        /// </summary>
        public async Task<bool> AddCustomerAddressAsync(string maKhachHang, DiaChiInfo newAddress)
        {
            try
            {
                var khachHang = await _context.KhachHangs.FirstOrDefaultAsync(k => k.MaKhachHang == maKhachHang);
                if (khachHang == null)
                    return false;

                var addresses = await GetCustomerAddressesAsync(maKhachHang);

                // Nếu là địa chỉ mặc định, bỏ mặc định các địa chỉ cũ
                if (newAddress.LaMacDinh)
                {
                    foreach (var addr in addresses)
                        addr.LaMacDinh = false;
                }

                // Nếu chưa có địa chỉ nào, đặt làm mặc định
                if (!addresses.Any())
                    newAddress.LaMacDinh = true;

                newAddress.Id = Guid.NewGuid().ToString();
                newAddress.ThoiGianTao = DateTime.Now;
                addresses.Add(newAddress);

                khachHang.DiaChiGiaoHang = JsonSerializer.Serialize(addresses);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding address for customer {MaKhachHang}", maKhachHang);
                return false;
            }
        }

        /// <summary>
        /// Xóa địa chỉ
        /// </summary>
        public async Task<bool> DeleteCustomerAddressAsync(string maKhachHang, string addressId)
        {
            try
            {
                var khachHang = await _context.KhachHangs.FirstOrDefaultAsync(k => k.MaKhachHang == maKhachHang);
                if (khachHang == null)
                    return false;

                var addresses = await GetCustomerAddressesAsync(maKhachHang);
                var addressToRemove = addresses.FirstOrDefault(a => a.Id == addressId);

                if (addressToRemove == null)
                    return false;

                addresses.Remove(addressToRemove);

                // Nếu xóa địa chỉ mặc định và còn địa chỉ khác, đặt địa chỉ đầu tiên làm mặc định
                if (addressToRemove.LaMacDinh && addresses.Any())
                {
                    addresses.First().LaMacDinh = true;
                }

                khachHang.DiaChiGiaoHang = JsonSerializer.Serialize(addresses);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting address for customer {MaKhachHang}", maKhachHang);
                return false;
            }
        }

        #endregion

        #region Shipping & Payment

        /// <summary>
        /// Tính phí vận chuyển
        /// </summary>
        public decimal CalculateShippingFee(string tinhThanh, string phuongThucVanChuyen, decimal tongTienHang)
        {
            // Kiểm tra miễn phí vận chuyển
            if (tongTienHang >= ShoppingConfiguration.DON_HANG_TOI_THIEU_MIEN_PHI_SHIP)
                return 0;

            // Lấy phí theo phương thức vận chuyển
            if (ShoppingConfiguration.PhuongThucVanChuyen.TryGetValue(phuongThucVanChuyen, out var method))
            {
                var phi = method.Phi;

                // Điều chỉnh phí theo khu vực
                if (tinhThanh.Contains("Hà Nội") || tinhThanh.Contains("TP. Hồ Chí Minh"))
                {
                    return phi; // Phí chuẩn cho Hà Nội/TP.HCM
                }
                else if (tinhThanh.Contains("Đà Nẵng") || tinhThanh.Contains("Cần Thơ"))
                {
                    return phi + 10000; // Phí cao hơn cho các thành phố lớn khác
                }
                else
                {
                    return phi + 20000; // Phí cao nhất cho các tỉnh xa
                }
            }

            return 30000; // Phí mặc định
        }

        /// <summary>
        /// Lấy danh sách phương thức vận chuyển khả dụng
        /// </summary>
        public List<PhuongThucVanChuyenInfo> GetAvailableShippingMethods(string? tinhThanh = null)
        {
            var methods = ShoppingConfiguration.PhuongThucVanChuyen.Values.ToList();

            // Giao hàng hỏa tốc chỉ có ở Hà Nội và TP.HCM
            if (!string.IsNullOrEmpty(tinhThanh) && 
                !tinhThanh.Contains("Hà Nội") && 
                !tinhThanh.Contains("TP. Hồ Chí Minh"))
            {
                methods = methods.Where(m => m.Ma != "EXPRESS").ToList();
            }

            return methods;
        }

        /// <summary>
        /// Lấy danh sách phương thức thanh toán khả dụng
        /// </summary>
        public List<PhuongThucThanhToanInfo> GetAvailablePaymentMethods(decimal tongTien)
        {
            var methods = ShoppingConfiguration.PhuongThucThanhToan.Values.ToList();

            // Kiểm tra số tiền tối thiểu cho COD
            var codMethod = methods.FirstOrDefault(m => m.Ma == "COD");
            if (codMethod != null && tongTien < ShoppingConfiguration.SO_TIEN_TOI_THIEU_COD)
            {
                codMethod.KhaDung = false;
                codMethod.MoTa = $"Số tiền tối thiểu cho COD là {ShoppingConfiguration.SO_TIEN_TOI_THIEU_COD:N0}đ";
            }

            return methods;
        }

        /// <summary>
        /// Tính thời gian giao hàng dự kiến
        /// </summary>
        public DateTime CalculateExpectedDeliveryTime(string phuongThucVanChuyen)
        {
            var baseDate = DateTime.Now;

            return phuongThucVanChuyen switch
            {
                "STANDARD" => baseDate.AddDays(5),
                "FAST" => baseDate.AddDays(2),
                "EXPRESS" => baseDate.AddHours(4),
                "PICKUP" => baseDate,
                _ => baseDate.AddDays(3)
            };
        }

        /// <summary>
        /// Mô phỏng xử lý thanh toán
        /// </summary>
        public async Task<(bool Success, string Message, string? TransactionId)> ProcessPaymentAsync(
            string phuongThucThanhToan, 
            decimal tongTien, 
            ThongTinThanhToanMoiViewModel? thongTinThe = null)
        {
            try
            {
                string transactionId = GenerateTransactionId();

                // Xử lý theo phương thức thanh toán
                switch (phuongThucThanhToan)
                {
                    case "COD":
                        return (true, "Đặt hàng thành công. Bạn sẽ thanh toán khi nhận hàng.", transactionId);

                    case "ATM":
                    case "VISA":
                        return await ProcessCardPaymentAsync(tongTien, thongTinThe, transactionId);

                    case "MOMO":
                        return await ProcessEWalletPaymentAsync("MoMo", tongTien, transactionId);

                    case "ZALOPAY":
                        return await ProcessEWalletPaymentAsync("ZaloPay", tongTien, transactionId);

                    default:
                        return (false, "Phương thức thanh toán không được hỗ trợ", null);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment");
                return (false, "Có lỗi xảy ra trong quá trình thanh toán", null);
            }
        }

        #endregion

        #region Order History & Tracking

        /// <summary>
        /// Tạo lịch sử trạng thái đơn hàng (lưu vào trường JSON hoặc mô phỏng)
        /// </summary>
        public async Task<bool> CreateOrderTrackingAsync(string maHoaDon, string trangThai, string? moTa = null, string? viTri = null)
        {
            try
            {
                // Trong thực tế có thể lưu vào một trường JSON trong HoaDonSanPham
                // Hoặc sử dụng logging/external service
                
                var hoaDon = await _context.HoaDonSanPhams.FirstOrDefaultAsync(h => h.MaHoaDonSanPham == maHoaDon);
                if (hoaDon != null)
                {
                    hoaDon.TrangThai = trangThai;
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Order {MaHoaDon} status updated to {TrangThai}", maHoaDon, trangThai);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order tracking for {MaHoaDon}", maHoaDon);
                return false;
            }
        }

        /// <summary>
        /// Lấy lịch sử trạng thái đơn hàng (mô phỏng)
        /// </summary>
        public List<TrangThaiInfo> GetOrderTrackingHistory(HoaDonSanPham hoaDon)
        {
            var history = new List<TrangThaiInfo>();
            var currentTime = hoaDon.ThoiGianTao;

            // Mô phỏng lịch sử trạng thái dựa trên trạng thái hiện tại
            history.Add(new TrangThaiInfo
            {
                TrangThai = "Đã đặt",
                MoTa = "Đơn hàng đã được đặt thành công",
                ThoiGianCapNhat = currentTime,
                ViTri = "Hệ thống",
                BuocThu = 1
            });

            if (IsStatusReached(hoaDon.TrangThai, "Đang chuẩn bị"))
            {
                history.Add(new TrangThaiInfo
                {
                    TrangThai = "Đang chuẩn bị",
                    MoTa = "Đơn hàng đang được chuẩn bị",
                    ThoiGianCapNhat = currentTime.AddHours(2),
                    ViTri = "Kho hàng",
                    BuocThu = 2
                });
            }

            if (IsStatusReached(hoaDon.TrangThai, "Đang giao"))
            {
                history.Add(new TrangThaiInfo
                {
                    TrangThai = "Đang giao",
                    MoTa = "Đơn hàng đang được giao đến bạn",
                    ThoiGianCapNhat = currentTime.AddDays(1),
                    ViTri = "Đang trên đường giao",
                    BuocThu = 3
                });
            }

            if (IsStatusReached(hoaDon.TrangThai, "Đã giao"))
            {
                history.Add(new TrangThaiInfo
                {
                    TrangThai = "Đã giao",
                    MoTa = "Đơn hàng đã được giao thành công",
                    ThoiGianCapNhat = currentTime.AddDays(2),
                    ViTri = "Đã giao đến khách hàng",
                    BuocThu = 4
                });
            }

            return history;
        }

        /// <summary>
        /// Chuẩn bị dữ liệu mua lại đơn hàng
        /// </summary>
        public async Task<ReorderViewModel?> PrepareReorderAsync(string maKhachHang, string maHoaDonGoc)
        {
            try
            {
                var hoaDonGoc = await _context.HoaDonSanPhams
                    .Include(h => h.ChiTietHoaDonSanPhams)
                        .ThenInclude(c => c.SanPham)
                    .FirstOrDefaultAsync(h => h.MaHoaDonSanPham == maHoaDonGoc && h.MaKhachHang == maKhachHang);

                if (hoaDonGoc == null)
                    return null;

                var reorderItems = new List<ReorderItemViewModel>();
                int soSanPhamKhongConHang = 0;
                decimal tongTienHienTai = 0;

                foreach (var chiTiet in hoaDonGoc.ChiTietHoaDonSanPhams)
                {
                    var sanPhamHienTai = await _context.SanPhams
                        .FirstOrDefaultAsync(s => s.MaSanPham == chiTiet.MaSanPham);

                    bool conHang = sanPhamHienTai != null && 
                                   sanPhamHienTai.TrangThai == "Còn hàng" && 
                                   sanPhamHienTai.SoLuongTon >= chiTiet.SoLuong;

                    if (!conHang)
                        soSanPhamKhongConHang++;

                    var giaHienTai = sanPhamHienTai?.Gia ?? 0;
                    if (conHang)
                        tongTienHienTai += giaHienTai * chiTiet.SoLuong;

                    reorderItems.Add(new ReorderItemViewModel
                    {
                        ChiTietGoc = chiTiet,
                        SanPhamHienTai = sanPhamHienTai,
                        ConHang = conHang,
                        GiaHienTai = giaHienTai,
                        GiaGoc = chiTiet.DonGia,
                        ChonMua = conHang,
                        SoLuongMua = conHang ? chiTiet.SoLuong : 0,
                        SoLuongGoc = chiTiet.SoLuong
                    });
                }

                return new ReorderViewModel
                {
                    MaHoaDonGoc = maHoaDonGoc,
                    HoaDonGoc = hoaDonGoc,
                    Items = reorderItems,
                    TongTienGoc = hoaDonGoc.TongTien,
                    TongTienHienTai = tongTienHienTai,
                    SoSanPhamKhongConHang = soSanPhamKhongConHang
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error preparing reorder for {MaHoaDon}", maHoaDonGoc);
                return null;
            }
        }

        #endregion

        #region Private Methods

        private string GenerateTransactionId()
        {
            return "TXN" + DateTime.Now.ToString("yyyyMMddHHmmss") + new Random().Next(1000, 9999);
        }

        private async Task<(bool Success, string Message, string TransactionId)> ProcessCardPaymentAsync(
            decimal tongTien, 
            ThongTinThanhToanMoiViewModel? thongTinThe, 
            string transactionId)
        {
            // Mô phỏng xử lý thanh toán thẻ
            await Task.Delay(2000);

            if (thongTinThe == null)
                return (false, "Thông tin thẻ không hợp lệ", transactionId);

            var random = new Random();
            var success = random.NextDouble() > 0.1; // 90% thành công

            if (success)
            {
                return (true, "Thanh toán thành công", transactionId);
            }
            else
            {
                return (false, "Thanh toán thất bại. Vui lòng kiểm tra lại thông tin thẻ.", transactionId);
            }
        }

        private async Task<(bool Success, string Message, string TransactionId)> ProcessEWalletPaymentAsync(
            string tenVi, 
            decimal tongTien, 
            string transactionId)
        {
            await Task.Delay(1500);

            var random = new Random();
            var success = random.NextDouble() > 0.05; // 95% thành công

            if (success)
            {
                return (true, $"Thanh toán qua {tenVi} thành công", transactionId);
            }
            else
            {
                return (false, $"Thanh toán qua {tenVi} thất bại. Vui lòng thử lại.", transactionId);
            }
        }

        private static bool IsStatusReached(string currentStatus, string targetStatus)
        {
            var statusOrder = new Dictionary<string, int>
            {
                { "Đã đặt", 1 },
                { "Đang chuẩn bị", 2 },
                { "Đang giao", 3 },
                { "Đã giao", 4 },
                { "Đã hủy", -1 }
            };

            return statusOrder.TryGetValue(currentStatus, out int current) &&
                   statusOrder.TryGetValue(targetStatus, out int target) &&
                   current >= target;
        }

        #endregion
    }
} 