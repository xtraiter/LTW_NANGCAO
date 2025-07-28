-- Sample product data for testing
USE RapChieuPhim_BA
GO

-- Insert sample products
INSERT INTO SanPham (maSanPham, tenSanPham, moTa, gia, soLuongTon, hinhAnh, trangThai, maNhanVien) VALUES
('SP001', N'Bắp rang bơ lớn', N'Bắp rang bơ thơm ngon, kích thước lớn phù hợp cho cả gia đình thưởng thức', 85000, 50, 'https://images.unsplash.com/photo-1578849278619-e73505e9610f?w=400', N'Còn hàng', 'NV001'),
('SP002', N'Nước ngọt Coca Cola', N'Nước giải khát Coca Cola 500ml, mát lạnh', 25000, 100, 'https://images.unsplash.com/photo-1554866585-cd94860890b7?w=400', N'Còn hàng', 'NV001'),
('SP003', N'Combo bắp nước', N'Combo bắp rang bơ vừa + nước ngọt tự chọn', 95000, 30, 'https://images.unsplash.com/photo-1489401015548-2b4b769f04e6?w=400', N'Còn hàng', 'NV001'),
('SP004', N'Kẹo dẻo Haribo', N'Kẹo dẻo nhiều vị thơm ngon, gói 200g', 45000, 25, 'https://images.unsplash.com/photo-1582058091505-f87a2e55a40f?w=400', N'Còn hàng', 'NV001'),
('SP005', N'Bánh mì kẹp thịt', N'Bánh mì kẹp thịt nướng BBQ với rau tươi', 65000, 15, 'https://images.unsplash.com/photo-1568901346375-23c9450c58cd?w=400', N'Còn hàng', 'NV001'),
('SP006', N'Trà sữa trân châu', N'Trá sữa trân châu đường đen thơm ngon', 55000, 20, 'https://images.unsplash.com/photo-1525385133512-2f3bdd039054?w=400', N'Còn hàng', 'NV001'),
('SP007', N'Áo thun D''CINE', N'Áo thun cotton có logo D''CINE độc đáo', 250000, 10, 'https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?w=400', N'Còn hàng', 'NV001'),
('SP008', N'Mũ lưỡi trai D''CINE', N'Mũ lưỡi trai thời trang với logo rạp chiếu phim', 150000, 12, 'https://images.unsplash.com/photo-1588850561407-ed78c282e89b?w=400', N'Còn hàng', 'NV001'),
('SP009', N'Ly giữ nhiệt D''CINE', N'Ly giữ nhiệt cao cấp in logo D''CINE', 120000, 8, 'https://images.unsplash.com/photo-1544145945-f90425340c7e?w=400', N'Còn hàng', 'NV001'),
('SP010', N'Móc khóa D''CINE', N'Móc khóa kim loại cao cấp hình logo rạp', 35000, 50, 'https://images.unsplash.com/photo-1586075010923-2dd4570fb338?w=400', N'Còn hàng', 'NV001'),
('SP011', N'Poster phim limited', N'Poster phim điện ảnh phiên bản giới hạn', 80000, 5, 'https://images.unsplash.com/photo-1489599387656-2bb4331f1c4c?w=400', N'Sắp hết', 'NV001'),
('SP012', N'Combo gia đình', N'Combo dành cho gia đình: 4 vé + bắp nước lớn', 450000, 3, 'https://images.unsplash.com/photo-1489401015548-2b4b769f04e6?w=400', N'Còn hàng', 'NV001');

-- Insert sample vouchers for products
INSERT INTO VoucherSanPham (maVoucherSanPham, tenVoucher, phanTramGiam, giaTriGiamToiDa, moTa, thoiGianBatDau, thoiGianKetThuc, soLuong, dieuKienApDung) VALUES
('VSP001', N'Giảm giá 10% cho khách hàng mới', 10, 50000, N'Voucher dành cho khách hàng mới lần đầu mua sắm', '2025-01-01', '2025-12-31', 100, N'Áp dụng cho đơn hàng từ 200,000 VNĐ'),
('VSP002', N'Giảm 15% cho combo', 15, 75000, N'Giảm giá đặc biệt cho các combo sản phẩm', '2025-01-01', '2025-06-30', 50, N'Áp dụng cho combo từ 300,000 VNĐ'),
('VSP003', N'Flash Sale 20%', 20, 100000, N'Flash sale cuối tuần giảm giá sốc', '2025-01-25', '2025-01-27', 25, N'Áp dụng cho tất cả sản phẩm');

GO 