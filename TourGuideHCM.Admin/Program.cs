using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Blazored.LocalStorage;
using TourGuideHCM.Admin;
using TourGuideHCM.Admin.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// MudBlazor
builder.Services.AddMudServices();

// LocalStorage — lưu JWT token
builder.Services.AddBlazoredLocalStorage();

// ====================== HttpClient + Auth Handler ======================
// API base URL — đổi khi deploy
const string ApiBaseUrl = "http://localhost:8080";

// Handler tự động attach Bearer token
builder.Services.AddScoped<AdminAuthorizedHttpHandler>();

// Named HttpClient với handler
builder.Services.AddHttpClient("API", client =>
{
    client.BaseAddress = new Uri(ApiBaseUrl);
})
.AddHttpMessageHandler<AdminAuthorizedHttpHandler>();

// Default HttpClient = API client (với token)
builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("API"));

// ====================== App Services ======================
builder.Services.AddScoped<AdminAuthService>();
builder.Services.AddScoped<PoiService>();
builder.Services.AddScoped<AudioService>();
builder.Services.AddScoped<AnalyticsService>();
builder.Services.AddScoped<PlaybackService>();
builder.Services.AddScoped<TtsQueueService>();
builder.Services.AddScoped<UserService>();

// ====================== MỚI ======================
builder.Services.AddScoped<DuplicateReportService>();
builder.Services.AddScoped<AudioRecoveryService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<POIApprovalService>();

Console.WriteLine("✅ Admin services initialized — API base: " + ApiBaseUrl);

var host = builder.Build();

// ====================== AUTO-LOGIN ADMIN ======================
// Thực hiện trước khi app chạy để đảm bảo mọi request về sau đều có token
using (var scope = host.Services.CreateScope())
{
    var auth = scope.ServiceProvider.GetRequiredService<AdminAuthService>();
    try
    {
        var ok = await auth.EnsureLoggedInAsync();
        if (!ok) Console.WriteLine("⚠️  Admin auto-login thất bại — các chức năng cần auth có thể không hoạt động");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️  Admin auto-login exception: {ex.Message}");
    }
}

await host.RunAsync();
