using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CinemaManagement.Data;
using CinemaManagement.Models;
using CinemaManagement.ViewModels;

namespace CinemaManagement.Controllers
{
    public class QuanLyKhuyenMaiController : Controller
    {
        private readonly CinemaDbContext _context;

        public QuanLyKhuyenMaiController(CinemaDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string searchTerm = "", string status = "", string sortBy = "tenGiamGia", int page = 1, int pageSize = 10)
        {
            // Kiểm tra quyền truy cập
            var role = HttpContext.Session.GetString("Role");
            if (role != "Quản lý" && role != "Nhân viên")
            {
                return RedirectToAction("Index", "Home");
            }

            var query = _context.Vouchers
                .Include(v => v.NhanVien)
                .AsQueryable();

            // Tìm kiếm
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(v => v.TenGiamGia.Contains(searchTerm) || 
                                        v.MaGiamGia.Contains(searchTerm) ||
                                        v.MoTa.Contains(searchTerm));
            }

            // Lọc theo trạng thái
            var currentDate = DateTime.Now;
            if (!string.IsNullOrEmpty(status))
            {
                switch (status.ToLower())
                {
                    case "active":
                        query = query.Where(v => v.ThoiGianBatDau <= currentDate && v.ThoiGianKetThuc >= currentDate);
                        break;
                    case "expired":
                        query = query.Where(v => v.ThoiGianKetThuc < currentDate);
                        break;
                    case "upcoming":
                        query = query.Where(v => v.ThoiGianBatDau > currentDate);
                        break;
                }
            }

            // Sắp xếp
            query = sortBy.ToLower() switch
            {
                "tengiamgia" => query.OrderBy(v => v.TenGiamGia),
                "phantramgiam" => query.OrderByDescending(v => v.PhanTramGiam),
                "thoigianbatdau" => query.OrderBy(v => v.ThoiGianBatDau),
                "thoigianketthuc" => query.OrderBy(v => v.ThoiGianKetThuc),
                "magiamgia" => query.OrderBy(v => v.MaGiamGia),
                _ => query.OrderBy(v => v.TenGiamGia)
            };

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var vouchers = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new QuanLyKhuyenMaiViewModel
            {
                Vouchers = vouchers,
                SearchTerm = searchTerm,
                Status = status,
                SortBy = sortBy,
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Quản lý")
            {
                return RedirectToAction("Index", "Home");
            }

            var nhanViens = await _context.NhanViens.ToListAsync();
            ViewBag.NhanViens = nhanViens;

            return View(new Voucher());
        }

        [HttpGet]
        public async Task<IActionResult> TestCreate()
        {
            var nhanViens = await _context.NhanViens.ToListAsync();
            ViewBag.NhanViens = nhanViens;
            
            // Test database connection
            ViewBag.TestMessage = $"Kết nối DB thành công. Tìm thấy {nhanViens.Count} nhân viên.";
            
            return View(new Voucher());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TestCreate(Voucher model)
        {
            // Loại bỏ validation cho navigation property
            ModelState.Remove("NhanVien");
            
            Console.WriteLine("=== TEST CREATE VOUCHER ===");
            Console.WriteLine($"TenGiamGia: '{model.TenGiamGia}'");
            Console.WriteLine($"PhanTramGiam: {model.PhanTramGiam}");
            Console.WriteLine($"MaNhanVien: '{model.MaNhanVien}'");
            Console.WriteLine($"ThoiGianBatDau: {model.ThoiGianBatDau}");
            Console.WriteLine($"ThoiGianKetThuc: {model.ThoiGianKetThuc}");
            Console.WriteLine($"MoTa: '{model.MoTa}'");

            // Kiểm tra đơn giản
            if (string.IsNullOrEmpty(model.TenGiamGia))
            {
                ModelState.AddModelError(nameof(model.TenGiamGia), "Tên khuyến mãi là bắt buộc");
            }

            if (model.PhanTramGiam <= 0 || model.PhanTramGiam > 100)
            {
                ModelState.AddModelError(nameof(model.PhanTramGiam), "Phần trăm giảm từ 1-100");
            }

            if (string.IsNullOrEmpty(model.MaNhanVien))
            {
                ModelState.AddModelError(nameof(model.MaNhanVien), "Chọn nhân viên");
            }

            if (model.ThoiGianBatDau >= model.ThoiGianKetThuc)
            {
                ModelState.AddModelError(nameof(model.ThoiGianKetThuc), "Thời gian kết thúc phải sau thời gian bắt đầu");
            }

            Console.WriteLine($"ModelState.IsValid: {ModelState.IsValid}");

            if (ModelState.IsValid)
            {
                try
                {
                    // Tạo mã test
                    var testId = $"TEST{DateTime.Now:HHmmss}";
                    model.MaGiamGia = testId;
                    
                    // Đảm bảo navigation property không được set
                    model.NhanVien = null;
                    
                    Console.WriteLine($"Attempting to save with MaGiamGia: {model.MaGiamGia}");
                    
                    _context.Vouchers.Add(model);
                    var result = await _context.SaveChangesAsync();
                    
                    Console.WriteLine($"SaveChanges returned: {result}");

                    TempData["SuccessMessage"] = $"Test tạo khuyến mãi thành công! Mã: {model.MaGiamGia}";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception occurred: {ex.Message}");
                    Console.WriteLine($"Inner Exception: {ex.InnerException?.Message}");
                    Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                    
                    ModelState.AddModelError("", $"Lỗi: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        ModelState.AddModelError("", $"Chi tiết: {ex.InnerException.Message}");
                    }
                }
            }
            else
            {
                Console.WriteLine("ModelState Errors:");
                foreach (var error in ModelState)
                {
                    Console.WriteLine($"  {error.Key}: {string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage))}");
                }
            }

            var nhanViens = await _context.NhanViens.ToListAsync();
            ViewBag.NhanViens = nhanViens;
            ViewBag.TestMessage = "Test create failed - check console for details";
            
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Voucher model)
        {
            // Loại bỏ validation cho navigation property
            ModelState.Remove("NhanVien");
            
            // Debug: Log thông tin model nhận được
            Console.WriteLine($"Model received - TenGiamGia: {model.TenGiamGia}, PhanTramGiam: {model.PhanTramGiam}, MaNhanVien: {model.MaNhanVien}");
            Console.WriteLine($"ThoiGianBatDau: {model.ThoiGianBatDau}, ThoiGianKetThuc: {model.ThoiGianKetThuc}");

            // Validate dates
            if (model.ThoiGianBatDau >= model.ThoiGianKetThuc)
            {
                ModelState.AddModelError("ThoiGianKetThuc", "Thời gian kết thúc phải sau thời gian bắt đầu");
            }

            // Validate nhân viên exists
            var nhanVienExists = await _context.NhanViens.AnyAsync(n => n.MaNhanVien == model.MaNhanVien);
            if (!nhanVienExists)
            {
                ModelState.AddModelError("MaNhanVien", "Nhân viên không tồn tại");
            }

            // Debug: Log validation state
            Console.WriteLine($"ModelState.IsValid: {ModelState.IsValid}");
            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState)
                {
                    Console.WriteLine($"Key: {error.Key}, Errors: {string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage))}");
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Tạo mã giảm giá tự động
                    var existingVouchers = await _context.Vouchers
                        .Where(v => v.MaGiamGia.StartsWith("VC"))
                        .Select(v => v.MaGiamGia)
                        .ToListAsync();

                    int nextNumber = 1;
                    if (existingVouchers.Any())
                    {
                        var numbers = existingVouchers
                            .Where(v => v.Length == 5 && v.Substring(2).All(char.IsDigit))
                            .Select(v => int.Parse(v.Substring(2)))
                            .ToList();
                        
                        if (numbers.Any())
                        {
                            nextNumber = numbers.Max() + 1;
                        }
                    }

                    model.MaGiamGia = $"VC{nextNumber:D3}";
                    Console.WriteLine($"Generated MaGiamGia: {model.MaGiamGia}");

                    // Kiểm tra trùng mã (tránh race condition)
                    var duplicateCheck = await _context.Vouchers.AnyAsync(v => v.MaGiamGia == model.MaGiamGia);
                    if (duplicateCheck)
                    {
                        // Tìm số tiếp theo
                        for (int i = nextNumber + 1; i <= 999; i++)
                        {
                            var testCode = $"VC{i:D3}";
                            if (!await _context.Vouchers.AnyAsync(v => v.MaGiamGia == testCode))
                            {
                                model.MaGiamGia = testCode;
                                break;
                            }
                        }
                    }

                    Console.WriteLine($"Final MaGiamGia: {model.MaGiamGia}");

                    // Đảm bảo navigation property không được set
                    model.NhanVien = null;

                    _context.Vouchers.Add(model);
                    var saveResult = await _context.SaveChangesAsync();
                    
                    Console.WriteLine($"SaveChanges result: {saveResult}");

                    if (saveResult > 0)
                    {
                        TempData["SuccessMessage"] = $"Thêm khuyến mãi '{model.TenGiamGia}' thành công với mã {model.MaGiamGia}!";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        ModelState.AddModelError("", "Không thể lưu khuyến mãi vào cơ sở dữ liệu");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception: {ex.Message}");
                    Console.WriteLine($"Inner Exception: {ex.InnerException?.Message}");
                    Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                    
                    if (ex.InnerException != null)
                    {
                        ModelState.AddModelError("", $"Lỗi cơ sở dữ liệu: {ex.InnerException.Message}");
                    }
                    else
                    {
                        ModelState.AddModelError("", $"Có lỗi xảy ra khi tạo khuyến mãi: {ex.Message}");
                    }
                }
            }

            // Reload danh sách nhân viên nếu có lỗi
            var nhanViens = await _context.NhanViens.ToListAsync();
            ViewBag.NhanViens = nhanViens;
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Quản lý")
            {
                return RedirectToAction("Index", "Home");
            }

            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher == null)
            {
                return NotFound();
            }

            var nhanViens = await _context.NhanViens.ToListAsync();
            ViewBag.NhanViens = nhanViens;

            return View(voucher);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Voucher model)
        {
            if (id != model.MaGiamGia)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Validate dates
                if (model.ThoiGianBatDau >= model.ThoiGianKetThuc)
                {
                    ModelState.AddModelError("ThoiGianKetThuc", "Thời gian kết thúc phải sau thời gian bắt đầu");
                    var nhanViens = await _context.NhanViens.ToListAsync();
                    ViewBag.NhanViens = nhanViens;
                    return View(model);
                }

                try
                {
                    _context.Update(model);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật khuyến mãi thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await VoucherExists(model.MaGiamGia))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            var nhanViensReload = await _context.NhanViens.ToListAsync();
            ViewBag.NhanViens = nhanViensReload;
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var voucher = await _context.Vouchers
                .Include(v => v.NhanVien)
                .Include(v => v.HDVouchers)
                    .ThenInclude(hv => hv.HoaDon)
                        .ThenInclude(h => h.KhachHang)
                .FirstOrDefaultAsync(v => v.MaGiamGia == id);

            if (voucher == null)
            {
                return NotFound();
            }

            return View(voucher);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Quản lý")
            {
                return Json(new { success = false, message = "Bạn không có quyền xóa khuyến mãi!" });
            }

            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher == null)
            {
                return Json(new { success = false, message = "Không tìm thấy khuyến mãi!" });
            }

            try
            {
                // Kiểm tra xem voucher có đang được sử dụng không
                var hasUsage = await _context.HDVouchers.AnyAsync(hv => hv.MaGiamGia == id);

                if (hasUsage)
                {
                    return Json(new { success = false, message = "Không thể xóa khuyến mãi này vì đã được sử dụng!" });
                }

                _context.Vouchers.Remove(voucher);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Xóa khuyến mãi thành công!" });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa khuyến mãi!" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Quản lý")
            {
                return Json(new { success = false, message = "Bạn không có quyền thay đổi trạng thái!" });
            }

            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher == null)
            {
                return Json(new { success = false, message = "Không tìm thấy khuyến mãi!" });
            }

            try
            {
                var currentDate = DateTime.Now;
                
                // Logic để toggle trạng thái (có thể điều chỉnh thời gian)
                if (voucher.ThoiGianKetThuc < currentDate)
                {
                    // Nếu đã hết hạn, gia hạn thêm 30 ngày
                    voucher.ThoiGianKetThuc = currentDate.AddDays(30);
                }
                else if (voucher.ThoiGianBatDau > currentDate)
                {
                    // Nếu chưa bắt đầu, bắt đầu ngay
                    voucher.ThoiGianBatDau = currentDate;
                }
                else
                {
                    // Nếu đang hoạt động, kết thúc ngay
                    voucher.ThoiGianKetThuc = currentDate.AddMinutes(-1);
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Cập nhật trạng thái thành công!" });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi cập nhật trạng thái!" });
            }
        }

        private async Task<bool> VoucherExists(string id)
        {
            return await _context.Vouchers.AnyAsync(e => e.MaGiamGia == id);
        }

        public string GetVoucherStatus(Voucher voucher)
        {
            var currentDate = DateTime.Now;
            
            if (voucher.ThoiGianKetThuc < currentDate)
                return "Hết hạn";
            else if (voucher.ThoiGianBatDau > currentDate)
                return "Sắp diễn ra";
            else
                return "Đang hoạt động";
        }
    }
}
