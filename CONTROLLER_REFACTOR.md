# Tách QuanLyController

Đã tách QuanLyController thành các controller chuyên biệt để code dễ quản lý hơn:

## Cấu trúc mới:

### 1. **DashboardController** (`/Controllers/DashboardController.cs`)
- **Chức năng**: Quản lý trang chủ thống kê và dashboard
- **Action chính**: 
  - `Index()` - Trang dashboard chính với thống kê tổng quan
  - `GetDoanhThuData()` - API lấy dữ liệu doanh thu cho biểu đồ
- **View**: `/Views/Dashboard/Index.cshtml`

### 2. **ThongKeController** (`/Controllers/ThongKeController.cs`)
- **Chức năng**: Quản lý các báo cáo và thống kê chi tiết
- **Actions**:
  - `ChiTiet()` - Thống kê chi tiết theo phim, phòng
  - `BaoCao()` - Báo cáo tổng hợp
- **Views**: 
  - `/Views/ThongKe/ChiTiet.cshtml`
  - `/Views/ThongKe/BaoCao.cshtml`

### 3. **PhimController** (`/Controllers/PhimController.cs`)
- **Chức năng**: Quản lý phim
- **Actions**:
  - `Index()` - Danh sách phim
  - `ThemPhim()` - Thêm phim mới
  - `XoaPhim()` - Xóa phim
  - `ChiTietPhim()` - Xem chi tiết phim
- **View**: `/Views/Phim/Index.cshtml`

### 4. **LichChieuController** (`/Controllers/LichChieuController.cs`)
- **Chức năng**: Quản lý lịch chiếu
- **Actions**:
  - `Index()` - Danh sách lịch chiếu
- **View**: `/Views/LichChieu/Index.cshtml`

### 5. **NhanVienManagementController** (`/Controllers/NhanVienManagementController.cs`)
- **Chức năng**: Quản lý nhân viên
- **Actions**:
  - `Index()` - Danh sách nhân viên
- **View**: `/Views/NhanVienManagement/Index.cshtml`

### 6. **QuanLyController** (đã được rút gọn)
- **Chức năng**: Chỉ còn làm redirect cho backward compatibility
- **Actions**: Tất cả đều redirect tới controller tương ứng

## Lợi ích:

1. **Tách bạch chức năng**: Mỗi controller chỉ quản lý một chức năng cụ thể
2. **Dễ bảo trì**: Code được tổ chức rõ ràng, dễ tìm và sửa lỗi
3. **Tái sử dụng**: Các phương thức helper được tách riêng, dễ tái sử dụng
4. **Mở rộng**: Dễ dàng thêm tính năng mới cho từng module
5. **Backward compatibility**: Các URL cũ vẫn hoạt động nhờ redirect

## Cách sử dụng:

- Truy cập dashboard: `/Dashboard` hoặc `/QuanLy` (redirect)
- Quản lý phim: `/Phim` hoặc `/QuanLy/QuanLyPhim` (redirect)  
- Thống kê chi tiết: `/ThongKe/ChiTiet` hoặc `/QuanLy/ThongKeChiTiet` (redirect)
- Báo cáo: `/ThongKe/BaoCao` hoặc `/QuanLy/BaoCao` (redirect)
- Quản lý lịch chiếu: `/LichChieu` hoặc `/QuanLy/QuanLyLichChieu` (redirect)
- Quản lý nhân viên: `/NhanVienManagement` hoặc `/QuanLy/QuanLyNhanVien` (redirect)

## Lưu ý:
- Tất cả các View đã được copy sang thư mục tương ứng
- Các API actions vẫn hoạt động bình thường
- Session và permission checking được giữ nguyên
- Database context injection được cấu hình cho tất cả controller mới
