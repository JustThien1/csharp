using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Blazored.LocalStorage;
using TourGuideHCM.Saler;
using TourGuideHCM.Saler.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// MudBlazor
builder.Services.AddMudServices();

// LocalStorage cho lưu JWT token
builder.Services.AddBlazoredLocalStorage();

// Auth handler tự động attach token vào mỗi HTTP request
builder.Services.AddScoped<AuthorizedHttpHandler>();

// HttpClient có base URL trỏ tới API backend
// ⚠️ Đổi URL khi deploy hoặc khi test trên thiết bị thật (dùng IP LAN)
const string ApiBaseUrl = "http://localhost:8080";

builder.Services.AddHttpClient("API", client =>
{
    client.BaseAddress = new Uri(ApiBaseUrl);
})
.AddHttpMessageHandler<AuthorizedHttpHandler>();

// Default HttpClient = API client
builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("API"));

// ====================== App Services ======================
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<PoiService>();
builder.Services.AddScoped<AudioService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<SubscriptionService>();
builder.Services.AddScoped<SubscriptionStateService>();

// Blazor Auth State
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();

Console.WriteLine("✅ Saler app initialized — API base: " + ApiBaseUrl);

await builder.Build().RunAsync();
