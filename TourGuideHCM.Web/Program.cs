using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Thêm dịch vụ cần thiết
builder.Services.AddControllersWithViews();   // Hỗ trợ Razor nếu sau này cần

var app = builder.Build();

// ====================== CẤU HÌNH QUAN TRỌNG ======================

// Cho phép phục vụ các file tĩnh (html, css, js, audio, images...) trong thư mục wwwroot
app.UseStaticFiles();

// Routing
app.UseRouting();

// Định tuyến các trang
app.UseEndpoints(endpoints =>
{
    // Trang mặc định (root) sẽ tự động chuyển sang trang quản trị
    endpoints.MapGet("/", () => Results.Redirect("/admin.html"));

    // Truy cập trực tiếp /admin
    endpoints.MapGet("/admin", async context =>
    {
        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.SendFileAsync("wwwroot/admin.html");
    });

    // Cho phép truy cập trực tiếp file admin.html
    endpoints.MapFallbackToFile("/admin.html", "admin.html");

    // Nếu sau này có Razor Pages hoặc Controller thì dùng dòng này
    // endpoints.MapControllers();
});

app.Run();