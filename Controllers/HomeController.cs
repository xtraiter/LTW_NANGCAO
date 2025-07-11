using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using CinemaManagement.Models;

namespace CinemaManagement.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        // Kiểm tra đăng nhập
        var maNhanVien = HttpContext.Session.GetString("MaNhanVien");
        if (string.IsNullOrEmpty(maNhanVien))
        {
            return RedirectToAction("Login", "Auth");
        }

        // Nếu đã đăng nhập, chuyển đến trang bán vé
        return RedirectToAction("Index", "BanVe");
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
