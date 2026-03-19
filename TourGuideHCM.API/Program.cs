using TourGuideHCM.API.Repositories;
using TourGuideHCM.API.Services;

var builder = WebApplication.CreateBuilder(args);

// ======================
// SERVICES
// ======================

builder.Services.AddControllers()
    .AddNewtonsoftJson(); // 🔥 FIX JSON

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DI
builder.Services.AddScoped<IPOIRepository, POIRepository>();
builder.Services.AddScoped<POIService>();

var app = builder.Build();

// ======================
// MIDDLEWARE
// ======================

app.UseSwagger();
app.UseSwaggerUI();

app.UseStaticFiles();

app.MapControllers();

// Test route
app.MapGet("/", () => "API OK");

app.Run();