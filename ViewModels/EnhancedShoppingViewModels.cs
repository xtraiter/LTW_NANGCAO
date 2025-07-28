using CinemaManagement.Models;
using System.ComponentModel.DataAnnotations;

namespace CinemaManagement.ViewModels
{
    // ViewModel cho địa chỉ giao hàng (sử dụng trường DiaChiGiaoHang trong KhachHang/HoaDonSanPham)
    public class DiaChiGiaoHangViewModel
    {
        [Required(ErrorMessage = "Tên người nhận là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên người nhận không được vượt quá 100 ký tự")]
        public string TenNguoiNhan { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string SoDienThoai { get; set; } = string.Empty;

        [Required(ErrorMessage = "Địa chỉ chi tiết là bắt buộc")]
        [StringLength(255, ErrorMessage = "Địa chỉ chi tiết không được vượt quá 255 ký tự")]
        public string DiaChiChiTiet { get; set; } = string.Empty;

        public string? PhuongXa { get; set; }
        public string? QuanHuyen { get; set; }
        public string? TinhThanh { get; set; }

        public bool LaMacDinh { get; set; } = false;

        // Danh sách địa chỉ đã lưu (JSON trong trường DiaChiGiaoHang của KhachHang)
        public List<DiaChiInfo> DanhSachDiaChi { get; set; } = new List<DiaChiInfo>();
    }

    public class DiaChiInfo
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string TenNguoiNhan { get; set; } = string.Empty;
        public string SoDienThoai { get; set; } = string.Empty;
        public string DiaChiChiTiet { get; set; } = string.Empty;
        public string? PhuongXa { get; set; }
        public string? QuanHuyen { get; set; }
        public string? TinhThanh { get; set; }
        public bool LaMacDinh { get; set; } = false;
        public DateTime ThoiGianTao { get; set; } = DateTime.Now;
    }

    // ViewModel cho thanh toán nâng cao
    public class EnhancedCheckoutViewModel
    {
        public GioHangViewModel GioHang { get; set; } = new GioHangViewModel();
        public KhachHang KhachHang { get; set; } = new KhachHang();

        // Địa chỉ giao hàng - 2 tùy chọn
        public bool SuDungDiaChiTaiKhoan { get; set; } = true; // Mặc định dùng địa chỉ từ tài khoản
        public string? DiaChiTaiKhoan { get; set; } // Địa chỉ có sẵn từ tài khoản
        
        // Địa chỉ nhập thủ công (khi không dùng địa chỉ tài khoản)
        public string? DiaChiGiaoHangMoi { get; set; }

        // Phương thức vận chuyển
        [Required(ErrorMessage = "Vui lòng chọn phương thức vận chuyển")]
        public string PhuongThucVanChuyen { get; set; } = "Giao hàng tiêu chuẩn";
        public List<PhuongThucVanChuyenInfo> DanhSachPhuongThucVC { get; set; } = new List<PhuongThucVanChuyenInfo>();

        // Phương thức thanh toán
        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán")]
        public string PhuongThucThanhToan { get; set; } = "COD";
        public List<PhuongThucThanhToanInfo> DanhSachPhuongThucTT { get; set; } = new List<PhuongThucThanhToanInfo>();

        // Thông tin thẻ thanh toán (nếu chọn thanh toán thẻ)
        public ThongTinThanhToanMoiViewModel? ThongTinThanhToan { get; set; }
        public bool LuuThongTinThanhToan { get; set; } = false;

        // Voucher và giảm giá
        public string? MaVoucher { get; set; }
        public List<VoucherSanPham> VouchersApDung { get; set; } = new List<VoucherSanPham>();

        // Tính toán phí
        public decimal TongTienHang { get; set; }
        public decimal PhiVanChuyen { get; set; }
        public decimal TienGiam { get; set; }
        public decimal TongThanhToan { get; set; }

        public string? GhiChu { get; set; }
    }

    // Thông tin phương thức vận chuyển
    public class PhuongThucVanChuyenInfo
    {
        public string Ma { get; set; } = string.Empty;
        public string Ten { get; set; } = string.Empty;
        public string MoTa { get; set; } = string.Empty;
        public string ThoiGianGiaoHang { get; set; } = string.Empty;
        public decimal Phi { get; set; }
        public bool KhaDung { get; set; } = true;
    }

    // Thông tin phương thức thanh toán
    public class PhuongThucThanhToanInfo
    {
        public string Ma { get; set; } = string.Empty;
        public string Ten { get; set; } = string.Empty;
        public string MoTa { get; set; } = string.Empty;
        public string LoaiThanhToan { get; set; } = string.Empty; // COD, ATM, Credit Card, E-Wallet
        public decimal PhiDichVu { get; set; } = 0;
        public bool KhaDung { get; set; } = true;
    }

    // ViewModel cho thông tin thẻ thanh toán mới
    public class ThongTinThanhToanMoiViewModel
    {
        [Required(ErrorMessage = "Tên trên thẻ là bắt buộc")]
        public string TenTrenThe { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số thẻ là bắt buộc")]
        [CreditCard(ErrorMessage = "Số thẻ không hợp lệ")]
        public string SoThe { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tháng hết hạn là bắt buộc")]
        [Range(1, 12, ErrorMessage = "Tháng hết hạn từ 1-12")]
        public int ThangHetHan { get; set; }

        [Required(ErrorMessage = "Năm hết hạn là bắt buộc")]
        [Range(2024, 2035, ErrorMessage = "Năm hết hạn không hợp lệ")]
        public int NamHetHan { get; set; }

        [Required(ErrorMessage = "Mã CVV là bắt buộc")]
        [StringLength(4, MinimumLength = 3, ErrorMessage = "Mã CVV từ 3-4 số")]
        public string CVV { get; set; } = string.Empty;

        public string? LoaiThe { get; set; }
        public string? TenNganHang { get; set; }
    }

    // ViewModel cho theo dõi đơn hàng
    public class TrackingOrderViewModel
    {
        public HoaDonSanPham HoaDon { get; set; } = new HoaDonSanPham();
        public List<TrangThaiInfo> LichSuTrangThai { get; set; } = new List<TrangThaiInfo>();
        public DiaChiInfo? DiaChiGiaoHang { get; set; }
        public List<ChiTietHoaDonSanPham> ChiTietDonHang { get; set; } = new List<ChiTietHoaDonSanPham>();

        // Thông tin vận chuyển
        public string? TrangThaiHienTai { get; set; }
        public string? ViTriHienTai { get; set; }
        public DateTime? ThoiGianCapNhatCuoi { get; set; }
        public DateTime? ThoiGianGiaoHangDuKien { get; set; }
        public string? GhiChu { get; set; }

        // Thông tin liên hệ
        public string? HotlineVanChuyen { get; set; } = "1900-1234";
        public string? EmailHoTro { get; set; } = "support@cinema.com";
    }

    public class TrangThaiInfo
    {
        public string TrangThai { get; set; } = string.Empty;
        public string? MoTa { get; set; }
        public DateTime ThoiGianCapNhat { get; set; }
        public string? ViTri { get; set; }
        public int BuocThu { get; set; }
    }

    // ViewModel cho lịch sử đơn hàng nâng cao
    public class EnhancedOrderHistoryViewModel
    {
        public List<HoaDonSanPhamDetailViewModel> DonHangs { get; set; } = new List<HoaDonSanPhamDetailViewModel>();
        
        // Filters
        public string? TrangThaiFilter { get; set; }
        public DateTime? TuNgay { get; set; }
        public DateTime? DenNgay { get; set; }
        public string? SearchTerm { get; set; }
        
        // Pagination
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }
        public int TotalOrders { get; set; }

        // Statistics
        public decimal TongChiTieu { get; set; }
        public int TongDonHang { get; set; }
        public int DonHangHoanThanh { get; set; }
        public int DonHangDangXuLy { get; set; }
    }

    public class HoaDonSanPhamDetailViewModel
    {
        public HoaDonSanPham HoaDon { get; set; } = new HoaDonSanPham();
        public List<ChiTietHoaDonSanPham> ChiTiet { get; set; } = new List<ChiTietHoaDonSanPham>();
        public List<TrangThaiInfo> LichSuTrangThai { get; set; } = new List<TrangThaiInfo>();
        public decimal TongTienTruocGiam { get; set; }
        public decimal TienGiam { get; set; }
        
        // Có thể mua lại không
        public bool CoTheMuaLai { get; set; } = true;
        
        // Có thể hủy/trả hàng không
        public bool CoTheHuy { get; set; } = false;
        public bool CoTheTraHang { get; set; } = false;
    }

    // ViewModel cho chức năng mua lại
    public class ReorderViewModel
    {
        public string MaHoaDonGoc { get; set; } = string.Empty;
        public HoaDonSanPham HoaDonGoc { get; set; } = new HoaDonSanPham();
        public List<ReorderItemViewModel> Items { get; set; } = new List<ReorderItemViewModel>();
        
        public decimal TongTienGoc { get; set; }
        public decimal TongTienHienTai { get; set; }
        public int SoSanPhamKhongConHang { get; set; }
        
        public bool TatCaSanPhamConHang => SoSanPhamKhongConHang == 0;
    }

    public class ReorderItemViewModel
    {
        public ChiTietHoaDonSanPham ChiTietGoc { get; set; } = new ChiTietHoaDonSanPham();
        public SanPham? SanPhamHienTai { get; set; }
        public bool ConHang { get; set; } = true;
        public decimal GiaHienTai { get; set; }
        public decimal GiaGoc { get; set; }
        public bool ChonMua { get; set; } = true;
        public int SoLuongMua { get; set; }
        public int SoLuongGoc { get; set; }
    }

    // ViewModel cho tính phí vận chuyển
    public class ShippingCalculatorViewModel
    {
        public string TinhThanh { get; set; } = string.Empty;
        public string QuanHuyen { get; set; } = string.Empty;
        public decimal TongTienHang { get; set; }
        public decimal PhiVanChuyen { get; set; }
        public List<PhuongThucVanChuyenInfo> DanhSachPhuongThuc { get; set; } = new List<PhuongThucVanChuyenInfo>();
        public bool MienPhiVanChuyen { get; set; } = false;
        public decimal SoTienConLaiDeMienPhi { get; set; }
        public decimal DonHangToiThieuMienPhi { get; set; } = 500000; // Cấu hình mặc định
    }

    // ViewModel cho giỏ hàng lưu trữ (sử dụng session)
    public class PersistentCartViewModel
    {
        public List<GioHangSanPhamItem> Items { get; set; } = new List<GioHangSanPhamItem>();
        public DateTime ThoiGianLuu { get; set; }
        public int SoNgayConLai { get; set; }
        public bool DaHetHan { get; set; } = false;
    }

    public class GioHangSanPhamItem
    {
        public string MaSanPham { get; set; } = string.Empty;
        public string TenSanPham { get; set; } = string.Empty;
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public string? HinhAnh { get; set; }
        public decimal ThanhTien => SoLuong * DonGia;
    }

    // Request models
    public class AddAddressRequest
    {
        [Required] public string TenNguoiNhan { get; set; } = string.Empty;
        [Required] public string SoDienThoai { get; set; } = string.Empty;
        [Required] public string DiaChiChiTiet { get; set; } = string.Empty;
        public string? PhuongXa { get; set; }
        public string? QuanHuyen { get; set; }
        public string? TinhThanh { get; set; }
        public bool LaMacDinh { get; set; } = false;
    }

    public class CalculateShippingRequest
    {
        [Required] public string TinhThanh { get; set; } = string.Empty;
        [Required] public string QuanHuyen { get; set; } = string.Empty;
        [Required] public decimal TongTienHang { get; set; }
    }

    public class PlaceOrderRequest
    {
        [Required] public string DiaChiGiaoHangId { get; set; } = string.Empty;
        [Required] public string PhuongThucVanChuyen { get; set; } = string.Empty;
        [Required] public string PhuongThucThanhToan { get; set; } = string.Empty;
        public string? MaVoucher { get; set; }
        public string? GhiChu { get; set; }
        public bool LuuThongTinThanhToan { get; set; } = false;
        public ThongTinThanhToanMoiViewModel? ThongTinThanhToan { get; set; }
    }

    public class EnhancedCheckoutRequest
    {
        public bool SuDungDiaChiTaiKhoan { get; set; } = true;
        public string? DiaChiGiaoHangMoi { get; set; }
        public string PhuongThucVanChuyen { get; set; } = "STANDARD";
        public string PhuongThucThanhToan { get; set; } = "COD";
        public string? MaVoucher { get; set; }
        public ThongTinThanhToanMoiViewModel? ThongTinThanhToan { get; set; }
        public string? GhiChu { get; set; }
    }

    // Configuration constants
    public static class ShoppingConfiguration
    {
        public static readonly Dictionary<string, PhuongThucVanChuyenInfo> PhuongThucVanChuyen = new()
        {
            { "STANDARD", new PhuongThucVanChuyenInfo { Ma = "STANDARD", Ten = "Giao hàng tiêu chuẩn", MoTa = "Giao hàng trong 3-5 ngày làm việc", ThoiGianGiaoHang = "3-5 ngày", Phi = 30000 } },
            { "FAST", new PhuongThucVanChuyenInfo { Ma = "FAST", Ten = "Giao hàng nhanh", MoTa = "Giao hàng trong 1-2 ngày làm việc", ThoiGianGiaoHang = "1-2 ngày", Phi = 50000 } },
            { "EXPRESS", new PhuongThucVanChuyenInfo { Ma = "EXPRESS", Ten = "Giao hàng hỏa tốc", MoTa = "Giao hàng trong 2-4 giờ (nội thành)", ThoiGianGiaoHang = "2-4 giờ", Phi = 100000 } },
            { "PICKUP", new PhuongThucVanChuyenInfo { Ma = "PICKUP", Ten = "Nhận tại cửa hàng", MoTa = "Khách hàng đến cửa hàng nhận trực tiếp", ThoiGianGiaoHang = "Ngay", Phi = 0 } }
        };

        public static readonly Dictionary<string, PhuongThucThanhToanInfo> PhuongThucThanhToan = new()
        {
            { "COD", new PhuongThucThanhToanInfo { Ma = "COD", Ten = "Thanh toán khi nhận hàng (COD)", MoTa = "Thanh toán bằng tiền mặt khi nhận hàng", LoaiThanhToan = "COD" } },
            { "ATM", new PhuongThucThanhToanInfo { Ma = "ATM", Ten = "Thẻ ATM nội địa", MoTa = "Thanh toán qua thẻ ATM của các ngân hàng Việt Nam", LoaiThanhToan = "ATM" } },
            { "VISA", new PhuongThucThanhToanInfo { Ma = "VISA", Ten = "Visa/MasterCard", MoTa = "Thanh toán qua thẻ tín dụng quốc tế", LoaiThanhToan = "Credit Card" } },
            { "MOMO", new PhuongThucThanhToanInfo { Ma = "MOMO", Ten = "Ví MoMo", MoTa = "Thanh toán qua ví điện tử MoMo", LoaiThanhToan = "E-Wallet" } },
            { "ZALOPAY", new PhuongThucThanhToanInfo { Ma = "ZALOPAY", Ten = "Ví ZaloPay", MoTa = "Thanh toán qua ví điện tử ZaloPay", LoaiThanhToan = "E-Wallet" } }
        };

        public const decimal DON_HANG_TOI_THIEU_MIEN_PHI_SHIP = 500000;
        public const int THOI_GIAN_LUU_GIO_HANG = 30; // ngày
        public const decimal SO_TIEN_TOI_THIEU_COD = 20000;
    }
} 