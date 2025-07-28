using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CinemaManagement.Data;
using CinemaManagement.Models;
using CinemaManagement.ViewModels;

namespace CinemaManagement.Controllers
{
    public class QuanLyKhachHangController : Controller
    {
        private readonly CinemaDbContext _context;

        public QuanLyKhachHangController(CinemaDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string searchTerm = "", string sortBy = "hoTen", int page = 1, int pageSize = 10)
        {
            // Kiểm tra quyền truy cập
            var role = HttpContext.Session.GetString("Role");
            if (role != "Quản lý" && role != "Nhân viên")
            {
                return RedirectToAction("Index", "Home");
            }

            var query = _context.KhachHangs.AsQueryable();

            // Tìm kiếm
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(k => k.HoTen.Contains(searchTerm) || 
                                        k.SDT.Contains(searchTerm) || 
                                        k.MaKhachHang.Contains(searchTerm));
            }

            // Sắp xếp
            query = sortBy.ToLower() switch
            {
                "hoten" => query.OrderBy(k => k.HoTen),
                "sdt" => query.OrderBy(k => k.SDT),
                "diemtichluy" => query.OrderByDescending(k => k.DiemTichLuy),
                "makhachhang" => query.OrderBy(k => k.MaKhachHang),
                _ => query.OrderBy(k => k.HoTen)
            };

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var khachHangs = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new QuanLyKhachHangViewModel
            {
                KhachHangs = khachHangs,
                SearchTerm = searchTerm,
                SortBy = sortBy,
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            };

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult Create()
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Quản lý" && role != "Nhân viên")
            {
                return RedirectToAction("Index", "Home");
            }

            return View(new KhachHang());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(KhachHang model)
        {
            if (ModelState.IsValid)
            {
                // Tạo mã khách hàng tự động
                var lastKhachHang = await _context.KhachHangs
                    .OrderByDescending(k => k.MaKhachHang)
                    .FirstOrDefaultAsync();

                if (lastKhachHang != null && lastKhachHang.MaKhachHang.StartsWith("KH"))
                {
                    var lastNumber = int.Parse(lastKhachHang.MaKhachHang.Substring(2));
                    model.MaKhachHang = $"KH{(lastNumber + 1):D3}";
                }
                else
                {
                    model.MaKhachHang = "KH001";
                }

                model.DiemTichLuy = 0; // Khách hàng mới bắt đầu với 0 điểm

                _context.KhachHangs.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Thêm khách hàng thành công!";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Quản lý" && role != "Nhân viên")
            {
                return RedirectToAction("Index", "Home");
            }

            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var khachHang = await _context.KhachHangs.FindAsync(id);
            if (khachHang == null)
            {
                return NotFound();
            }

            return View(khachHang);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, KhachHang model)
        {
            if (id != model.MaKhachHang)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(model);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật thông tin khách hàng thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await KhachHangExists(model.MaKhachHang))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var khachHang = await _context.KhachHangs
                .Include(k => k.TaiKhoans)
                .Include(k => k.HoaDons)
                    .ThenInclude(h => h.CTHDs)
                        .ThenInclude(c => c.Ve)
                .FirstOrDefaultAsync(k => k.MaKhachHang == id);

            if (khachHang == null)
            {
                return NotFound();
            }

            return View(khachHang);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Quản lý")
            {
                return Json(new { success = false, message = "Bạn không có quyền xóa khách hàng!" });
            }

            var khachHang = await _context.KhachHangs.FindAsync(id);
            if (khachHang == null)
            {
                return Json(new { success = false, message = "Không tìm thấy khách hàng!" });
            }

            try
            {
                // Kiểm tra xem khách hàng có đang được sử dụng không
                var hasOrders = await _context.HoaDons.AnyAsync(h => h.MaKhachHang == id);
                var hasAccount = await _context.TaiKhoans.AnyAsync(t => t.MaKhachHang == id);

                if (hasOrders || hasAccount)
                {
                    return Json(new { success = false, message = "Không thể xóa khách hàng này vì đang có dữ liệu liên quan!" });
                }

                _context.KhachHangs.Remove(khachHang);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Xóa khách hàng thành công!" });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa khách hàng!" });
            }
        }

        private async Task<bool> KhachHangExists(string id)
        {
            return await _context.KhachHangs.AnyAsync(e => e.MaKhachHang == id);
        }
    }
}
