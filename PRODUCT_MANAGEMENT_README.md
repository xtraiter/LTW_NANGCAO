# 🛒 Hệ thống Quản lý Sản phẩm và Mua sắm

## 📋 Tổng quan

Hệ thống quản lý sản phẩm và mua sắm đã được tích hợp vào dự án Quản lý Rạp Chiếu Phim, bao gồm:

### 🔧 **Tính năng cho Admin**
- ✅ Quản lý danh sách sản phẩm
- ✅ Thêm/sửa/xóa sản phẩm
- ✅ Cập nhật tồn kho trực tiếp
- ✅ Thống kê doanh thu từ sản phẩm
- ✅ Xem lịch sử bán hàng

### 🛍️ **Tính năng cho Khách hàng**
- ✅ Duyệt và tìm kiếm sản phẩm
- ✅ Thêm sản phẩm vào giỏ hàng
- ✅ Quản lý giỏ hàng
- ✅ Thanh toán đơn hàng
- ✅ Xem lịch sử đơn hàng
- ✅ Áp dụng voucher giảm giá

## 🚀 Cách chạy và test hệ thống

### 1. **Cài đặt Database**

Chạy script SQL để thêm dữ liệu mẫu:

```sql
-- Trong SQL Server Management Studio hoặc Azure Data Studio
-- Kết nối đến database RapChieuPhim_BA và chạy file:
-- SampleProductData.sql
```

### 2. **Chạy ứng dụng**

```bash
dotnet run
```

Ứng dụng sẽ chạy tại: `https://localhost:5001` hoặc `http://localhost:5000`

## 🎯 Hướng dẫn sử dụng chi tiết

### 📊 **Cho Admin (Quản lý)**

#### **Truy cập trang quản lý sản phẩm:**
1. Đăng nhập với tài khoản Admin
2. Vào menu **"Quản Lý"** → **"Quản lý sản phẩm"**
3. URL: `/SanPham`

#### **Các chức năng chính:**

**🔍 Xem danh sách sản phẩm:**
- Hiển thị thống kê tổng quan (tổng sản phẩm, còn hàng, hết hàng, giá trị tồn kho)
- Bộ lọc theo tên, trạng thái, khoảng giá
- Cập nhật tồn kho trực tiếp bằng AJAX

**➕ Thêm sản phẩm mới:**
- Vào `/SanPham/Create`
- Điền thông tin: mã, tên, mô tả, giá, số lượng, hình ảnh, trạng thái
- Preview hình ảnh trực tiếp khi nhập URL

**✏️ Sửa sản phẩm:**
- Click biểu tượng ✏️ ở danh sách sản phẩm
- Chỉnh sửa thông tin (mã sản phẩm không thể thay đổi)

**👁️ Xem chi tiết sản phẩm:**
- Click biểu tượng 👁️ ở danh sách sản phẩm
- Xem thống kê chi tiết: số lượng đã bán, doanh thu, lịch sử bán hàng

**🗑️ Xóa sản phẩm:**
- Click biểu tượng 🗑️ ở danh sách sản phẩm
- Xác nhận xóa (chỉ xóa được nếu chưa có đơn hàng nào)

### 🛒 **Cho Khách hàng**

#### **Truy cập cửa hàng:**
1. Đăng nhập với tài khoản Khách hàng
2. Vào menu **"Cửa hàng"**
3. URL: `/KhachHang/Shopping`

#### **Các chức năng chính:**

**🔍 Mua sắm:**
- Duyệt danh sách sản phẩm với hình ảnh đẹp mắt
- Tìm kiếm theo tên sản phẩm
- Lọc theo khoảng giá
- Sắp xếp theo tên hoặc giá
- Xem sản phẩm mới và bán chạy

**🛍️ Thêm vào giỏ hàng:**
- Click **"Thêm vào giỏ"** trên sản phẩm
- Hoặc vào chi tiết sản phẩm để chọn số lượng

**👁️ Chi tiết sản phẩm:**
- Click **"Chi tiết"** hoặc hình ảnh sản phẩm
- Xem mô tả đầy đủ, chọn số lượng
- Xem sản phẩm liên quan

**🛒 Quản lý giỏ hàng:**
- Menu **"Giỏ hàng"** hoặc URL: `/KhachHang/GioHang`
- Cập nhật số lượng, xóa sản phẩm
- Áp dụng voucher giảm giá
- Xem tổng tiền

**💳 Thanh toán:**
- Từ giỏ hàng, click **"Thanh toán"**
- Nhập địa chỉ giao hàng
- Xác nhận đơn hàng

**📋 Lịch sử đơn hàng:**
- Menu **"Đơn hàng"** hoặc URL: `/KhachHang/LichSuDonHang`
- Xem tất cả đơn hàng đã đặt
- Lọc theo trạng thái, ngày
- Chi tiết từng đơn hàng

## 🗂️ Cấu trúc File đã tạo

### **Models:**
```
Models/
├── SanPham.cs                    # Model sản phẩm
├── GioHang.cs                    # Model giỏ hàng
├── ChiTietGioHang.cs            # Model chi tiết giỏ hàng
├── HoaDonSanPham.cs             # Model hóa đơn sản phẩm
├── ChiTietHoaDonSanPham.cs      # Model chi tiết hóa đơn
├── VoucherSanPham.cs            # Model voucher sản phẩm
├── HoaDonSanPhamVoucher.cs      # Model liên kết hóa đơn-voucher
└── YeuCauHoanTra.cs             # Model yêu cầu hoàn trả
```

### **ViewModels:**
```
ViewModels/
└── SanPhamViewModels.cs         # Tất cả ViewModels cho sản phẩm
```

### **Controllers:**
```
Controllers/
├── SanPhamController.cs         # Controller quản lý sản phẩm (Admin)
└── KhachHangController.cs       # Đã thêm tính năng mua sắm
```

### **Views:**
```
Views/
├── SanPham/                     # Views cho Admin
│   ├── Index.cshtml            # Danh sách sản phẩm
│   ├── Create.cshtml           # Thêm sản phẩm
│   ├── Edit.cshtml             # Sửa sản phẩm
│   ├── Details.cshtml          # Chi tiết sản phẩm
│   └── Delete.cshtml           # Xóa sản phẩm
└── KhachHang/                   # Views cho Khách hàng
    ├── Shopping.cshtml          # Trang mua sắm
    ├── ChiTietSanPham.cshtml    # Chi tiết sản phẩm
    ├── GioHang.cshtml           # Giỏ hàng
    ├── ThanhToanSanPham.cshtml  # Thanh toán
    └── LichSuDonHang.cshtml     # Lịch sử đơn hàng
```

### **Styles:**
```
wwwroot/css/
└── shopping.css                 # CSS tùy chỉnh cho giao diện mua sắm
```

## 📊 Database Schema

### **Bảng chính được thêm:**

1. **SanPham** - Thông tin sản phẩm
2. **GioHang** - Giỏ hàng của khách hàng
3. **ChiTietGioHang** - Chi tiết sản phẩm trong giỏ hàng
4. **HoaDonSanPham** - Hóa đơn mua sản phẩm
5. **ChiTietHoaDonSanPham** - Chi tiết hóa đơn sản phẩm
6. **VoucherSanPham** - Voucher giảm giá sản phẩm
7. **HoaDonSanPham_Voucher** - Liên kết hóa đơn với voucher
8. **YeuCauHoanTra** - Yêu cầu hoàn trả sản phẩm

## 🎨 Giao diện

### **Thiết kế hiện đại:**
- ✅ Responsive design cho mobile, tablet, desktop
- ✅ Hover effects và animations mượt mà
- ✅ Loading states và feedback tương tác
- ✅ Toast notifications cho các hành động
- ✅ Color coding cho trạng thái đơn hàng
- ✅ Product cards với lazy loading images

### **UX/UI Features:**
- ✅ Breadcrumb navigation
- ✅ Real-time cart updates
- ✅ Product image preview
- ✅ Quantity selectors
- ✅ Price formatting
- ✅ Status badges
- ✅ Empty states

## 🔐 Bảo mật và Validation

### **Authentication:**
- ✅ Session-based authentication
- ✅ Role-based access control
- ✅ CSRF protection với ValidateAntiForgeryToken

### **Data Validation:**
- ✅ Server-side validation
- ✅ Client-side validation với jQuery
- ✅ Input sanitization
- ✅ Business logic validation

### **Security Features:**
- ✅ SQL injection prevention với Entity Framework
- ✅ XSS protection
- ✅ Secure image URL validation
- ✅ Stock validation trước khi đặt hàng

## 🧪 Test Cases

### **Test chức năng Admin:**
1. ✅ Đăng nhập với tài khoản admin
2. ✅ Thêm sản phẩm mới với đầy đủ thông tin
3. ✅ Cập nhật tồn kho trực tiếp từ danh sách
4. ✅ Sửa thông tin sản phẩm
5. ✅ Xem chi tiết và thống kê sản phẩm
6. ✅ Xóa sản phẩm (test với sản phẩm có/không có đơn hàng)

### **Test chức năng Khách hàng:**
1. ✅ Đăng nhập với tài khoản khách hàng
2. ✅ Duyệt danh sách sản phẩm, sử dụng bộ lọc
3. ✅ Thêm sản phẩm vào giỏ hàng
4. ✅ Cập nhật số lượng trong giỏ hàng
5. ✅ Xóa sản phẩm khỏi giỏ hàng
6. ✅ Thanh toán đơn hàng với địa chỉ giao hàng
7. ✅ Xem lịch sử đơn hàng và chi tiết

### **Test tích hợp:**
1. ✅ Admin thêm sản phẩm → Khách hàng thấy sản phẩm mới
2. ✅ Khách hàng mua hàng → Tồn kho tự động giảm
3. ✅ Khách hàng mua hết tồn kho → Sản phẩm tự động chuyển "Hết hàng"

## 🚨 Troubleshooting

### **Lỗi thường gặp:**

**1. Build errors:**
```bash
# Nếu thiếu ViewModels
dotnet build
# Kiểm tra Views/_ViewImports.cshtml có @using CinemaManagement.ViewModels
```

**2. Database errors:**
```bash
# Nếu thiếu bảng sản phẩm
# Chạy script SampleProductData.sql trong SQL Server
```

**3. 404 Not Found:**
```bash
# Kiểm tra URL routes:
# Admin: /SanPham, /SanPham/Create, /SanPham/Edit/{id}
# Customer: /KhachHang/Shopping, /KhachHang/GioHang
```

**4. Session timeout:**
```bash
# Đăng nhập lại nếu session hết hạn
# Kiểm tra role permissions
```

## 🔄 Future Enhancements

### **Có thể phát triển thêm:**
- 📱 Mobile app integration
- 💳 Payment gateway integration (PayOS)
- 📧 Email notifications cho đơn hàng
- 🔔 Push notifications
- 📊 Advanced analytics và reports
- 🎯 Product recommendations AI
- 📦 Inventory management automation
- 🏪 Multi-vendor support
- 🌍 Multi-language support
- 📱 QR code scanning

---

## 📞 Hỗ trợ

Nếu gặp vấn đề, hãy kiểm tra:
1. ✅ Đã chạy script SQL để thêm dữ liệu mẫu
2. ✅ Đã build project không có lỗi
3. ✅ Database connection string đúng
4. ✅ Đăng nhập với đúng role (Admin/Khách hàng)

**Happy Shopping! 🛒✨** 