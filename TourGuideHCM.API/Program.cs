using Microsoft.EntityFrameworkCore;
using TourGuideHCM.API.Data;
using TourGuideHCM.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "TourGuideHCM API", Version = "v1" });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader());
});

builder.Services.AddScoped<POIService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TourGuideHCM API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// ====================== Khởi tạo Database ======================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();

        context.Database.EnsureCreated();
        DbSeeder.Seed(context);

        Console.WriteLine("✅ Kết nối Database thành công với Login 'tourguide'!");
        Console.WriteLine($"   Số POI: {context.POIs.Count()}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Lỗi kết nối Database: {ex.Message}");
        if (ex.InnerException != null)
            Console.WriteLine($"   Chi tiết: {ex.InnerException.Message}");
    }
}

app.Run();