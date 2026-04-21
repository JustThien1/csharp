using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TourGuideHCM.App.Models;
using TourGuideHCM.App.Services.Interfaces;

namespace TourGuideHCM.App.ViewModels;

/// <summary>
/// ViewModel cho HomePage — trang chính hiển thị:
/// - Header có GPS location + thời tiết
/// - Hành trình cá nhân (stats)
/// - Danh mục POI
/// - Audio guide nổi bật
/// - Danh sách POI gần nhất (live distance)
/// </summary>
public class HomeViewModel : INotifyPropertyChanged
{
    private readonly IApiService _api;
    private readonly IDatabaseService _db;

    public ObservableCollection<POI> NearbyPois { get; } = new();
    public ObservableCollection<CategoryItem> Categories { get; } = new();

    // ====================== Location & Weather ======================
    private string _locationName = "Đang xác định vị trí…";
    public string LocationName
    {
        get => _locationName;
        set { _locationName = value; OnPropertyChanged(); }
    }

    private string _weatherText = "—";
    public string WeatherText
    {
        get => _weatherText;
        set { _weatherText = value; OnPropertyChanged(); }
    }

    private string _weatherIcon = "☀️";
    public string WeatherIcon
    {
        get => _weatherIcon;
        set { _weatherIcon = value; OnPropertyChanged(); }
    }

    // ====================== Stats hành trình cá nhân ======================
    private int _poiVisitedCount;
    public int PoiVisitedCount
    {
        get => _poiVisitedCount;
        set { _poiVisitedCount = value; OnPropertyChanged(); }
    }

    private double _totalKmWalked;
    public double TotalKmWalked
    {
        get => _totalKmWalked;
        set { _totalKmWalked = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalKmDisplay)); }
    }
    public string TotalKmDisplay => TotalKmWalked.ToString("F1");

    private int _audioListenedCount;
    public int AudioListenedCount
    {
        get => _audioListenedCount;
        set { _audioListenedCount = value; OnPropertyChanged(); }
    }

    // ====================== Header counts ======================
    private int _nearbyCount;
    public int NearbyCount
    {
        get => _nearbyCount;
        set { _nearbyCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(NearbyCountText)); }
    }
    public string NearbyCountText => $"📍 {NearbyCount} điểm gần bạn";

    private int _totalAudioCount;
    public int TotalAudioCount
    {
        get => _totalAudioCount;
        set { _totalAudioCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalAudioText)); }
    }
    public string TotalAudioText => $"🎧 {TotalAudioCount} audio";

    // ====================== Loading state ======================
    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; OnPropertyChanged(); }
    }

    // ====================== Commands ======================
    public ICommand RefreshCommand { get; }
    public ICommand OpenCategoryCommand { get; }
    public ICommand OpenPoiCommand { get; }

    public HomeViewModel(IApiService api, IDatabaseService db)
    {
        _api = api;
        _db = db;

        RefreshCommand = new Command(async () => await LoadAllAsync());
        OpenCategoryCommand = new Command<CategoryItem>(async (c) =>
        {
            if (c == null) return;
            await Shell.Current.GoToAsync($"///PoiListPage?category={c.Id}");
        });
        OpenPoiCommand = new Command<POI>(async (poi) =>
        {
            if (poi == null) return;
            await Shell.Current.GoToAsync($"///MapPage?poiId={poi.Id}");
        });

        InitDefaultCategories();
    }

    // ====================== LOAD DATA ======================
    public async Task LoadAllAsync()
    {
        IsLoading = true;
        try
        {
            // Song song: load POIs + location + weather
            var taskPois = LoadPoisAsync();
            var taskLocation = LoadLocationAsync();
            var taskStats = LoadStatsAsync();

            await Task.WhenAll(taskPois, taskLocation, taskStats);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ HomeVM Load error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadPoisAsync()
    {
        try
        {
            var pois = await _api.GetPoisAsync();
            if (pois == null || !pois.Any()) return;

            // Lấy location hiện tại để tính distance
            Location? currentLoc = null;
            try
            {
                currentLoc = await Geolocation.Default.GetLastKnownLocationAsync()
                             ?? await Geolocation.Default.GetLocationAsync(
                                 new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(5)));
            }
            catch { /* không có GPS cũng chấp nhận */ }

            if (currentLoc != null)
            {
                foreach (var poi in pois)
                {
                    poi.DistanceMeters = Haversine(currentLoc.Latitude, currentLoc.Longitude, poi.Lat, poi.Lng);
                }
            }

            // Chỉ lấy POI trong bán kính 2km (nếu có GPS), sắp xếp theo khoảng cách
            var nearby = pois
                .Where(p => p.IsActive)
                .Where(p => p.DistanceMeters == null || p.DistanceMeters.Value < 2000)
                .OrderBy(p => p.DistanceMeters ?? double.MaxValue)
                .Take(10)
                .ToList();

            NearbyPois.Clear();
            foreach (var p in nearby)
            {
                // Gán gradient + emoji theo category để card đẹp hơn
                AssignCardStyle(p);
                NearbyPois.Add(p);
            }

            NearbyCount = nearby.Count;
            TotalAudioCount = pois.Count(p => !string.IsNullOrEmpty(p.AudioUrl));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠ LoadPois: {ex.Message}");
        }
    }

    private async Task LoadLocationAsync()
    {
        try
        {
            var loc = await Geolocation.Default.GetLastKnownLocationAsync();
            if (loc == null)
            {
                LocationName = "Chưa xác định vị trí";
                return;
            }

            // Reverse geocoding để lấy tên quận
            try
            {
                var placemarks = await Geocoding.Default.GetPlacemarksAsync(loc);
                var p = placemarks?.FirstOrDefault();
                if (p != null)
                {
                    var subLocality = p.SubLocality ?? p.Locality ?? p.AdminArea ?? "TP.HCM";
                    LocationName = $"📍 {subLocality}, TP.HCM";
                }
                else
                {
                    LocationName = "📍 TP.HCM";
                }
            }
            catch
            {
                LocationName = "📍 TP.HCM";
            }

            // Load thời tiết từ open-meteo (miễn phí, không cần API key)
            await LoadWeatherAsync(loc.Latitude, loc.Longitude);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠ LoadLocation: {ex.Message}");
            LocationName = "📍 TP.HCM";
        }
    }

    private async Task LoadWeatherAsync(double lat, double lng)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var url = $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lng}&current=temperature_2m,weather_code";
            var json = await http.GetFromJsonAsync<WeatherResponse>(url);

            if (json?.Current != null)
            {
                var temp = (int)Math.Round(json.Current.Temperature2m);
                var (icon, desc) = WeatherCodeToText(json.Current.WeatherCode);
                WeatherIcon = icon;
                WeatherText = $"{temp}°C · {desc}";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠ LoadWeather: {ex.Message}");
            WeatherText = "29°C · Nắng";
            WeatherIcon = "☀️";
        }
    }

    private async Task LoadStatsAsync()
    {
        try
        {
            // Lấy playback history từ local DB
            // - POI đã thăm = số POI unique đã trigger
            // - Audio đã nghe = tổng số lượt phát audio
            var history = await _db.GetPlaybackHistoryAsync(1000);

            PoiVisitedCount = history.Select(h => h.PoiId).Distinct().Count();
            AudioListenedCount = history.Count;

            // Tổng km đi — chưa có data tracking chuẩn → tạm để 0
            // Nếu muốn tính thật: query LocationHistory từ DB và tính Haversine liên tiếp
            TotalKmWalked = 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠ LoadStats: {ex.Message}");
            PoiVisitedCount = 0;
            AudioListenedCount = 0;
            TotalKmWalked = 0;
        }
    }

    // ====================== Helpers ======================
    private void InitDefaultCategories()
    {
        Categories.Clear();
        Categories.Add(new CategoryItem { Id = 1, Name = "Di tích", Icon = "🏛", GradientStart = "#FF9A8B", GradientEnd = "#FF6A88" });
        Categories.Add(new CategoryItem { Id = 2, Name = "Ẩm thực", Icon = "🍜", GradientStart = "#F093FB", GradientEnd = "#F5576C" });
        Categories.Add(new CategoryItem { Id = 3, Name = "Mua sắm", Icon = "🛍", GradientStart = "#FA709A", GradientEnd = "#FEE140" });
        Categories.Add(new CategoryItem { Id = 4, Name = "Cafe", Icon = "☕", GradientStart = "#84FAB0", GradientEnd = "#8FD3F4" });
        Categories.Add(new CategoryItem { Id = 5, Name = "Quán ăn", Icon = "🍲", GradientStart = "#FFC371", GradientEnd = "#FF5F6D" });
        Categories.Add(new CategoryItem { Id = 6, Name = "Nhà hàng", Icon = "🍽", GradientStart = "#A18CD1", GradientEnd = "#FBC2EB" });
        Categories.Add(new CategoryItem { Id = 7, Name = "Khách sạn", Icon = "🏨", GradientStart = "#30CFD0", GradientEnd = "#330867" });
        Categories.Add(new CategoryItem { Id = 8, Name = "Công viên", Icon = "🌳", GradientStart = "#43E97B", GradientEnd = "#38F9D7" });
    }

    private static void AssignCardStyle(POI poi)
    {
        // Gán icon + gradient theo CategoryId
        var (icon, gStart, gEnd) = poi.CategoryId switch
        {
            1 => ("⛪", "#FF9A8B", "#FF6A88"),
            2 => ("🍜", "#F093FB", "#F5576C"),
            3 => ("🛍", "#FA709A", "#FEE140"),
            4 => ("☕", "#84FAB0", "#8FD3F4"),
            5 => ("🍲", "#FFC371", "#FF5F6D"),
            6 => ("🍽", "#A18CD1", "#FBC2EB"),
            7 => ("🏨", "#30CFD0", "#330867"),
            8 => ("🌳", "#43E97B", "#38F9D7"),
            _ => ("📍", "#667EEA", "#764BA2"),
        };
        poi.IconEmoji = icon;
        poi.GradientStart = gStart;
        poi.GradientEnd = gEnd;
    }

    private static double Haversine(double lat1, double lng1, double lat2, double lng2)
    {
        const double R = 6371000; // meters
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLng = (lng2 - lng1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180)
              * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static (string icon, string desc) WeatherCodeToText(int code) => code switch
    {
        0 => ("☀️", "Nắng"),
        1 or 2 => ("🌤", "Nắng ít mây"),
        3 => ("☁️", "Nhiều mây"),
        45 or 48 => ("🌫", "Sương mù"),
        51 or 53 or 55 or 56 or 57 => ("🌦", "Mưa phùn"),
        61 or 63 or 65 => ("🌧", "Mưa"),
        66 or 67 => ("🌧", "Mưa rét"),
        71 or 73 or 75 or 77 => ("❄️", "Tuyết"),
        80 or 81 or 82 => ("🌧", "Mưa rào"),
        85 or 86 => ("🌨", "Tuyết rào"),
        95 => ("⛈", "Giông"),
        96 or 99 => ("⛈", "Giông kèm mưa đá"),
        _ => ("🌡", "Thời tiết")
    };

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    // ====================== Helper classes ======================
    public class CategoryItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Icon { get; set; } = "";
        public string GradientStart { get; set; } = "#FF6B6B";
        public string GradientEnd { get; set; } = "#FF8E53";
    }

    private class WeatherResponse
    {
        public CurrentWeather? Current { get; set; }
    }
    private class CurrentWeather
    {
        public double Temperature2m { get; set; }
        public int WeatherCode { get; set; }
    }
}