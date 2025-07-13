using Microsoft.EntityFrameworkCore;
using CinemaManagement.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add Entity Framework
builder.Services.AddDbContext<CinemaDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Tạo khách hàng GUEST nếu chưa tồn tại
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<CinemaDbContext>();
    try
    {
        // Tạo nhân viên GUEST
        var guestEmployee = await context.NhanViens.FindAsync("GUEST");
        if (guestEmployee == null)
        {
            context.NhanViens.Add(new CinemaManagement.Models.NhanVien
            {
                MaNhanVien = "GUEST",
                TenNhanVien = "Hệ thống",
                ChucVu = "Tự động",
                SDT = "0000000000",
                NgaySinh = DateTime.Now
            });
            await context.SaveChangesAsync();
            Console.WriteLine("Đã tạo nhân viên GUEST cho hệ thống");
        }

        // Tạo khách hàng GUEST
        var guestCustomer = await context.KhachHangs.FindAsync("GUEST");
        if (guestCustomer == null)
        {
            context.KhachHangs.Add(new CinemaManagement.Models.KhachHang
            {
                MaKhachHang = "GUEST",
                HoTen = "Khách lẻ",
                SDT = "0000000000",
                DiemTichLuy = 0
            });
            await context.SaveChangesAsync();
            Console.WriteLine("Đã tạo khách hàng GUEST cho khách lẻ");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Lỗi khi tạo dữ liệu GUEST: {ex.Message}");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseSession();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
