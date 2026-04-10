using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using TourGuideHCM.Admin;
using TourGuideHCM.Admin.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// MudBlazor
builder.Services.AddMudServices();

// HttpClient
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri("http://localhost:5284")
});

// Services
builder.Services.AddScoped<PoiService>();
builder.Services.AddScoped<AnalyticsService>(); // 👈 QUAN TRỌNG
// Thêm dòng này cùng với các service khác
builder.Services.AddScoped<AudioService>();
Console.WriteLine("🔥 ANALYTICS SERVICE REGISTERED 🔥");
await builder.Build().RunAsync();