namespace TourGuideHCM.App.Services;

/// <summary>
/// Tất cả string UI — đổi ngôn ngữ bằng cách gọi LanguageService.SetLanguage()
/// </summary>
public static class AppLanguage
{
    // ── Login ─────────────────────────────────────────────────────────────────
    public static string LoginTitle => L("Đăng nhập", "Sign In");
    public static string Username => L("Tên đăng nhập", "Username");
    public static string Password => L("Mật khẩu", "Password");
    public static string LoginBtn => L("Đăng nhập", "Sign In");
    public static string GuestBtn => L("Tiếp tục với tư cách khách", "Continue as Guest");
    public static string RegisterBtn => L("Đăng ký", "Register");
    public static string LoginError => L("Tên đăng nhập hoặc mật khẩu không đúng", "Invalid username or password");
    public static string LoginRequired => L("Vui lòng nhập tên đăng nhập và mật khẩu", "Please enter username and password");
    public static string ConnectError => L("Lỗi kết nối", "Connection error");

    // ── Register ──────────────────────────────────────────────────────────────
    public static string RegisterTitle => L("Đăng ký tài khoản", "Create Account");
    public static string FullName => L("Họ và tên", "Full Name");
    public static string Email => L("Email (tùy chọn)", "Email (optional)");
    public static string ConfirmPassword => L("Xác nhận mật khẩu", "Confirm Password");
    public static string RegisterSubmit => L("Tạo tài khoản", "Create Account");
    public static string RegisterSuccess => L("Tài khoản đã được tạo!", "Account created!");
    public static string PasswordShort => L("Mật khẩu phải có ít nhất 6 ký tự", "Password must be at least 6 characters");
    public static string PasswordMismatch => L("Mật khẩu xác nhận không khớp", "Passwords do not match");
    public static string FillRequired => L("Vui lòng điền đầy đủ thông tin bắt buộc (*)", "Please fill in all required fields (*)");

    // ── Map ───────────────────────────────────────────────────────────────────
    public static string MapTitle => L("Bản đồ tham quan", "Tour Map");
    public static string Loading => L("Đang tải điểm tham quan...", "Loading attractions...");
    public static string Loaded => L("Đã tải {0} điểm tham quan", "Loaded {0} attractions");
    public static string Offline => L("{0} điểm (offline)", "{0} points (offline)");
    public static string NoData => L("Không có dữ liệu", "No data available");
    public static string NearestPoint => L("📍 Điểm gần nhất", "📍 Nearest Point");
    public static string ListenBtn => L("▶ Nghe", "▶ Listen");
    public static string StopBtn => L("■ Dừng", "■ Stop");
    public static string Radius => L("📡 Bán kính: {0}", "📡 Radius: {0}");
    public static string NoPermission => L("Chưa cấp quyền vị trí – thuyết minh tự động bị tắt",
                                                "Location permission denied – auto narration disabled");

    // ── POI Detail ────────────────────────────────────────────────────────────
    public static string ListenNarration => L("▶  Nghe thuyết minh", "▶  Listen Narration");
    public static string Free => L("Miễn phí", "Free");
    public static string OpeningHours => L("Giờ mở cửa", "Opening Hours");

    // ── POI List ──────────────────────────────────────────────────────────────
    public static string PoiListTitle => L("Danh sách địa điểm", "Attractions");
    public static string SearchHint => L("Tìm kiếm...", "Search...");
    public static string AllCategories => L("Tất cả", "All");

    // ── App Shell / Flyout ────────────────────────────────────────────────────
    public static string FeaturedPlaces => L("Các địa điểm nổi bật", "Featured Places");
    public static string MenuMap => L("🗺️  Bản đồ", "🗺️  Map");
    public static string MenuPoiList => L("📋  Danh sách POI", "📋  POI List");
    public static string MenuLogout => L("🚪  Đăng xuất", "🚪  Logout");

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static string L(string vi, string en)
        => LanguageService.IsEnglish ? en : vi;
}
