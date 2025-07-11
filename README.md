# 🎬 CNPM_TH_BA - Hệ thống Quản lý Rạp Chiếu Phim

## 📋 Mô tả dự án

Hệ thống quản lý rạp chiếu phim là một ứng dụng web toàn diện được xây dựng bằng **ASP.NET Core 9.0** và **Entity Framework Core**, hỗ trợ đầy đủ các chức năng vận hành một rạp chiếu phim hiện đại.

## 🎯 Mục tiêu dự án

- **Số hóa quy trình quản lý**: Chuyển đổi từ quản lý thủ công sang hệ thống tự động
- **Tối ưu trải nghiệm khách hàng**: Đặt vé trực tuyến, chọn ghế dễ dàng
- **Quản lý hiệu quả**: Theo dõi doanh thu, thống kê chi tiết
- **Phân quyền rõ ràng**: Hệ thống phân quyền cho từng loại người dùng
- **Báo cáo thông minh**: Cung cấp các báo cáo chi tiết về hoạt động rạp chiếu

## 🛠️ Công nghệ sử dụng

### Backend
- **ASP.NET Core 9.0**: Framework web chính
- **Entity Framework Core 9.0**: ORM cho quản lý cơ sở dữ liệu
- **SQL Server**: Hệ quản trị cơ sở dữ liệu
- **C# (.NET 9.0)**: Ngôn ngữ lập trình chính

### Frontend
- **HTML5**: Xây dựng cấu trúc giao diện
- **CSS3**: Tạo kiểu và responsive design
- **JavaScript**: Tương tác động và xử lý phía client
- **Bootstrap**: Framework CSS cho giao diện responsive

### Packages & Libraries
- **Microsoft.EntityFrameworkCore.SqlServer**: Kết nối SQL Server
- **Microsoft.EntityFrameworkCore.Tools**: Công cụ migration
- **Microsoft.AspNetCore.Session**: Quản lý session
- **Microsoft.EntityFrameworkCore.Design**: Hỗ trợ thiết kế database

## 🎭 Tính năng chính

### 🎬 Quản lý Phim
- **Thêm/Sửa/Xóa phim**: Quản lý danh sách phim đang chiếu
- **Thông tin chi tiết**: Tên phim, thể loại, thời lượng, độ tuổi, mô tả
- **Poster & Media**: Quản lý hình ảnh và file đa phương tiện
- **Phân loại theo thể loại**: Hành động, tình cảm, kinh dị, khoa học viễn tưởng...

### 📅 Quản lý Lịch Chiếu
- **Tạo lịch chiếu**: Sắp xếp suất chiếu theo phim, phòng, thời gian
- **Quản lý suất chiếu**: Thêm, sửa, hủy các suất chiếu
- **Kiểm tra xung đột**: Tự động phát hiện xung đột lịch chiếu
- **Lịch chiếu linh hoạt**: Hỗ trợ nhiều suất chiếu trong ngày

### 🏢 Quản lý Phòng Chiếu & Ghế Ngồi
- **Quản lý phòng chiếu**: Thêm, sửa thông tin phòng chiếu
- **Sơ đồ ghế ngồi**: Thiết kế và quản lý sơ đồ ghế từng phòng
- **Loại ghế**: Ghế thường, ghế VIP, ghế đôi
- **Trạng thái ghế**: Trống, đã đặt, hỏng, bảo trì

### 🎫 Hệ thống Đặt Vé
- **Đặt vé trực tuyến**: Khách hàng chọn phim, suất chiếu, ghế ngồi
- **Chọn ghế tương tác**: Giao diện trực quan để chọn ghế
- **Thanh toán**: Tích hợp các phương thức thanh toán
- **In vé**: Tạo và in vé điện tử
- **Hủy vé**: Chức năng hủy vé theo quy định

### 👥 Quản lý Người Dùng
- **Khách hàng**: Đăng ký, đăng nhập, quản lý thông tin cá nhân
- **Nhân viên**: Quản lý bán vé, kiểm tra vé, hỗ trợ khách hàng
- **Quản lý**: Toàn quyền quản lý hệ thống
- **Phân quyền**: Hệ thống phân quyền chi tiết theo vai trò

### 🎁 Hệ thống Voucher & Khuyến mại
- **Tạo voucher**: Giảm giá theo phần trăm hoặc số tiền cố định
- **Quản lý khuyến mại**: Các chương trình khuyến mại đặc biệt
- **Áp dụng tự động**: Tự động áp dụng voucher phù hợp
- **Theo dõi sử dụng**: Thống kê hiệu quả các chương trình khuyến mại

### 📊 Báo cáo & Thống kê
- **Doanh thu**: Báo cáo doanh thu theo ngày, tuần, tháng
- **Thống kê phim**: Phim được xem nhiều nhất, ít nhất
- **Hiệu suất phòng chiếu**: Tỷ lệ lấp đầy ghế theo phòng
- **Thống kê khách hàng**: Phân tích hành vi khách hàng
- **Báo cáo chi tiết**: Xuất báo cáo Excel, PDF

## 🔧 Cấu trúc dự án

```text
CNPM_TH_BA/
├── Controllers/           # Các controller xử lý request
│   ├── AuthController.cs      # Xử lý đăng nhập/đăng ký
│   ├── BanVeController.cs     # Quản lý bán vé
│   ├── HomeController.cs      # Trang chủ
│   ├── PhatHanhVeController.cs # Phát hành vé
│   └── QuanLyController.cs    # Quản lý hệ thống
├── Models/               # Các model dữ liệu
│   ├── Phim.cs              # Model phim
│   ├── LichChieu.cs         # Model lịch chiếu
│   ├── PhongChieu.cs        # Model phòng chiếu
│   ├── GheNgoi.cs           # Model ghế ngồi
│   ├── Ve.cs                # Model vé
│   ├── KhachHang.cs         # Model khách hàng
│   ├── NhanVien.cs          # Model nhân viên
│   ├── TaiKhoan.cs          # Model tài khoản
│   ├── HoaDon.cs            # Model hóa đơn
│   └── Voucher.cs           # Model voucher
├── Views/                # Giao diện người dùng
│   ├── Auth/                # Trang đăng nhập
│   ├── BanVe/               # Giao diện bán vé
│   ├── PhatHanhVe/          # Giao diện phát hành vé
│   ├── QuanLy/              # Giao diện quản lý
│   └── Shared/              # Layout chung
├── Data/                 # Cấu hình database
│   └── CinemaDbContext.cs   # Entity Framework Context
├── ViewModels/           # View models
├── wwwroot/              # Static files (CSS, JS, images)
└── Properties/           # Cấu hình ứng dụng
```

## 🚀 Hướng dẫn cài đặt và chạy

### Yêu cầu hệ thống

- **.NET 9.0 SDK** trở lên
- **SQL Server** (LocalDB hoặc SQL Server Express)
- **Visual Studio 2022** hoặc **Visual Studio Code**
- **Git** để clone repository

### Cài đặt

1. **Clone repository**

   ```bash
   git clone https://github.com/xtraiter/CNPM_TH_BA.git
   cd CNPM_TH_BA
   ```

2. **Cài đặt dependencies**

   ```bash
   dotnet restore
   ```

3. **Cấu hình database**

   - Mở file `appsettings.json`
   - Cập nhật connection string theo môi trường của bạn:

   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=YOUR_SERVER;Database=RapChieuPhim;Trusted_Connection=true;TrustServerCertificate=true;"
     }
   }
   ```

4. **Tạo database**

   ```bash
   dotnet ef database update
   ```

5. **Chạy ứng dụng**

   ```bash
   dotnet run
   ```

   Hoặc sử dụng VS Code Task:
   - Mở VS Code
   - Nhấn `Ctrl+Shift+P` → chọn "Tasks: Run Task" → chọn "Run Cinema Management"

6. **Truy cập ứng dụng**

   Mở trình duyệt và truy cập: `https://localhost:5001`

### Cấu hình bổ sung

- **Entity Framework Tools**: Đã được cài đặt sẵn trong project
- **Session Management**: Đã được cấu hình với thời gian timeout 30 phút
- **HTTPS**: Được kích hoạt mặc định trong môi trường development

## 👥 Đối tượng sử dụng

### 🎭 Khách hàng
- Xem danh sách phim đang chiếu
- Đặt vé trực tuyến
- Chọn ghế ngồi
- Thanh toán online
- Quản lý lịch sử đặt vé
- Sử dụng voucher giảm giá

### 🏢 Nhân viên bán vé
- Bán vé trực tiếp tại quầy
- Kiểm tra và xác nhận vé
- Hỗ trợ khách hàng
- Xử lý các vấn đề liên quan đến vé
- Báo cáo doanh thu ca làm việc

### 👨‍💼 Quản lý rạp chiếu
- Quản lý toàn bộ hệ thống
- Thêm/sửa/xóa phim
- Tạo và quản lý lịch chiếu
- Quản lý nhân viên
- Xem báo cáo doanh thu chi tiết
- Tạo và quản lý voucher
- Cấu hình hệ thống

## 📈 Tính năng nổi bật

### 🎯 Giao diện thân thiện
- Thiết kế responsive, tương thích mọi thiết bị
- Giao diện trực quan, dễ sử dụng
- Tốc độ tải trang nhanh
- Hỗ trợ nhiều ngôn ngữ

### 🔒 Bảo mật cao
- Mã hóa mật khẩu
- Session management an toàn
- Phân quyền chặt chẽ
- Validation dữ liệu đầu vào

### 📊 Báo cáo thông minh
- Dashboard tổng quan
- Biểu đồ trực quan
- Xuất báo cáo Excel/PDF
- Thống kê theo thời gian thực

### 🔧 Dễ bảo trì
- Code structure rõ ràng
- Sử dụng Design Patterns
- Logging chi tiết
- Error handling toàn diện

## 🤝 Đóng góp vào dự án

### Quy trình đóng góp

1. **Fork repository**
2. **Tạo branch mới**

   ```bash
   git checkout -b feature/ten-tinh-nang-moi
   ```

3. **Commit changes**

   ```bash
   git commit -m "Thêm tính năng: [mô tả ngắn gọn]"
   ```

4. **Push to branch**

   ```bash
   git push origin feature/ten-tinh-nang-moi
   ```

5. **Tạo Pull Request**

### Coding Standards

- Sử dụng C# naming conventions
- Comment code rõ ràng
- Viết unit tests cho các chức năng mới
- Đảm bảo code clean và readable

## 🐛 Báo cáo lỗi

Nếu bạn gặp lỗi, hãy tạo issue mới với thông tin:

- **Mô tả lỗi**: Mô tả chi tiết lỗi xảy ra
- **Các bước tái hiện**: Liệt kê từng bước dẫn đến lỗi
- **Môi trường**: OS, trình duyệt, phiên bản .NET
- **Screenshots**: Đính kèm ảnh nếu có thể

## 📚 Tài liệu tham khảo

- [ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core/)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
- [Bootstrap Documentation](https://getbootstrap.com/docs/)
- [SQL Server Documentation](https://docs.microsoft.com/en-us/sql/sql-server/)

## 🔮 Kế hoạch phát triển

### Phase 1 (Đã hoàn thành) ✅
- ✅ Quản lý phim cơ bản
- ✅ Đặt vé trực tuyến
- ✅ Quản lý lịch chiếu
- ✅ Hệ thống thanh toán

### Phase 2 (Đang phát triển) 🚧
- 🚧 Tích hợp payment gateway
- 🚧 Mobile app
- 🚧 Hệ thống đánh giá phim
- 🚧 Tích hợp mạng xã hội

### Phase 3 (Kế hoạch) 📋
- 📋 AI recommendation system
- 📋 Loyalty program
- 📋 Multi-cinema management
- 📋 Advanced analytics

## 📄 Giấy phép

Dự án này được phát hành dưới giấy phép MIT License. Xem file [LICENSE](LICENSE) để biết thêm chi tiết.

## 👨‍💻 Thông tin liên hệ

- **Tác giả**: [xtraiter](https://github.com/xtraiter)
- **Email**: [Liên hệ qua GitHub](https://github.com/xtraiter)
- **Repository**: [CNPM_TH_BA](https://github.com/xtraiter/CNPM_TH_BA)
- **Issues**: [Báo cáo lỗi](https://github.com/xtraiter/CNPM_TH_BA/issues)

---

⭐ **Nếu dự án này hữu ích, hãy cho chúng tôi một star trên GitHub!** ⭐
