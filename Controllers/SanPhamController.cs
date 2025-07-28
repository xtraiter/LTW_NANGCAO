using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CinemaManagement.Data;
using CinemaManagement.Models;
using CinemaManagement.ViewModels;

namespace CinemaManagement.Controllers
{
    public class SanPhamController : BaseController
    {
        private readonly CinemaDbContext _context;

        public SanPhamController(CinemaDbContext context)
        {
            _context = context;
        }

        // GET: SanPham
        public async Task<IActionResult> Index(
            string? tenSanPham, 
            string? trangThai, 
            decimal? giaMin, 
            decimal? giaMax)
        {
            if (!IsEmployeeLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            var query = _context.SanPhams.Include(s => s.NhanVien).AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(tenSanPham))
            {
                query = query.Where(s => s.TenSanPham.Contains(tenSanPham));
            }

            if (!string.IsNullOrEmpty(trangThai))
            {
                query = query.Where(s => s.TrangThai == trangThai);
            }

            if (giaMin.HasValue)
            {
                query = query.Where(s => s.Gia >= giaMin.Value);
            }

            if (giaMax.HasValue)
            {
                query = query.Where(s => s.Gia <= giaMax.Value);
            }

            var sanPhams = await query.OrderBy(s => s.TenSanPham).ToListAsync();

            var viewModel = new QuanLySanPhamViewModel
            {
                SanPhams = sanPhams,
                TongSoSanPham = sanPhams.Count,
                SanPhamConHang = sanPhams.Count(s => s.TrangThai == "Còn hàng" && s.SoLuongTon > 0),
                SanPhamHetHang = sanPhams.Count(s => s.TrangThai == "Hết hàng" || s.SoLuongTon == 0),
                TongGiaTriTonKho = sanPhams.Sum(s => s.Gia * s.SoLuongTon),
                TenSanPham = tenSanPham,
                TrangThai = trangThai,
                GiaMin = giaMin,
                GiaMax = giaMax
            };

            return View(viewModel);
        }

        // GET: SanPham/Details/5
        public async Task<IActionResult> Details(string? id)
        {
            if (!IsEmployeeLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            if (id == null)
            {
                return NotFound();
            }

            var sanPham = await _context.SanPhams
                .Include(s => s.NhanVien)
                .Include(s => s.ChiTietHoaDonSanPhams)
                    .ThenInclude(c => c.HoaDonSanPham)
                .FirstOrDefaultAsync(m => m.MaSanPham == id);

            if (sanPham == null)
            {
                return NotFound();
            }

            var viewModel = new ChiTietSanPhamViewModel
            {
                SanPham = sanPham,
                LichSuBanHang = sanPham.ChiTietHoaDonSanPhams.ToList(),
                SoLuongDaBan = sanPham.ChiTietHoaDonSanPhams.Sum(c => c.SoLuong),
                DoanhThuTuSanPham = sanPham.ChiTietHoaDonSanPhams.Sum(c => c.SoLuong * c.DonGia)
            };

            return View(viewModel);
        }

        // GET: SanPham/Create
        public IActionResult Create()
        {
            if (!IsEmployeeLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            var viewModel = new CreateSanPhamViewModel
            {
                MaNhanVien = GetCurrentEmployeeId() ?? ""
            };

            return View(viewModel);
        }

        // POST: SanPham/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateSanPhamViewModel viewModel)
        {
            if (!IsEmployeeLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }

            // Check if product code already exists
            if (await _context.SanPhams.AnyAsync(s => s.MaSanPham == viewModel.MaSanPham))
            {
                ModelState.AddModelError("MaSanPham", "Mã sản phẩm đã tồn tại");
                return View(viewModel);
            }

            var sanPham = new SanPham
            {
                MaSanPham = viewModel.MaSanPham,
                TenSanPham = viewModel.TenSanPham,
                MoTa = viewModel.MoTa,
                Gia = viewModel.Gia,
                SoLuongTon = viewModel.SoLuongTon,
                HinhAnh = viewModel.HinhAnh,
                TrangThai = viewModel.TrangThai,
                MaNhanVien = GetCurrentEmployeeId() ?? ""
            };

            _context.Add(sanPham);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Tạo sản phẩm thành công!";
            return RedirectToAction(nameof(Index));
        }

        // GET: SanPham/Edit/5
        public async Task<IActionResult> Edit(string? id)
        {
            if (!IsEmployeeLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            if (id == null)
            {
                return NotFound();
            }

            var sanPham = await _context.SanPhams.FindAsync(id);
            if (sanPham == null)
            {
                return NotFound();
            }

            var viewModel = new CreateSanPhamViewModel
            {
                MaSanPham = sanPham.MaSanPham,
                TenSanPham = sanPham.TenSanPham,
                MoTa = sanPham.MoTa,
                Gia = sanPham.Gia,
                SoLuongTon = sanPham.SoLuongTon,
                HinhAnh = sanPham.HinhAnh,
                TrangThai = sanPham.TrangThai,
                MaNhanVien = sanPham.MaNhanVien
            };

            return View(viewModel);
        }

        // POST: SanPham/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, CreateSanPhamViewModel viewModel)
        {
            if (!IsEmployeeLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            if (id != viewModel.MaSanPham)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }

            try
            {
                var sanPham = await _context.SanPhams.FindAsync(id);
                if (sanPham == null)
                {
                    return NotFound();
                }

                sanPham.TenSanPham = viewModel.TenSanPham;
                sanPham.MoTa = viewModel.MoTa;
                sanPham.Gia = viewModel.Gia;
                sanPham.SoLuongTon = viewModel.SoLuongTon;
                sanPham.HinhAnh = viewModel.HinhAnh;
                sanPham.TrangThai = viewModel.TrangThai;

                _context.Update(sanPham);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SanPhamExists(viewModel.MaSanPham))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        // GET: SanPham/Delete/5
        public async Task<IActionResult> Delete(string? id)
        {
            if (!IsEmployeeLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            if (id == null)
            {
                return NotFound();
            }

            var sanPham = await _context.SanPhams
                .Include(s => s.NhanVien)
                .FirstOrDefaultAsync(m => m.MaSanPham == id);

            if (sanPham == null)
            {
                return NotFound();
            }

            return View(sanPham);
        }

        // POST: SanPham/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (!IsEmployeeLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            var sanPham = await _context.SanPhams.FindAsync(id);
            
            if (sanPham != null)
            {
                // Check if product is referenced in orders
                var hasOrders = await _context.ChiTietHoaDonSanPhams
                    .AnyAsync(c => c.MaSanPham == id);

                if (hasOrders)
                {
                    TempData["ErrorMessage"] = "Không thể xóa sản phẩm đã có đơn hàng!";
                    return RedirectToAction(nameof(Index));
                }

                _context.SanPhams.Remove(sanPham);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xóa sản phẩm thành công!";
            }

            return RedirectToAction(nameof(Index));
        }

        // AJAX endpoint to update stock
        [HttpPost]
        public async Task<IActionResult> UpdateStock(string maSanPham, int soLuongMoi)
        {
            if (!IsEmployeeLoggedIn())
            {
                return Json(new { success = false, message = "Không có quyền truy cập" });
            }

            try
            {
                var sanPham = await _context.SanPhams.FindAsync(maSanPham);
                if (sanPham == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm" });
                }

                sanPham.SoLuongTon = soLuongMoi;
                sanPham.TrangThai = soLuongMoi > 0 ? "Còn hàng" : "Hết hàng";

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Cập nhật tồn kho thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        private bool SanPhamExists(string id)
        {
            return _context.SanPhams.Any(e => e.MaSanPham == id);
        }
    }
} 