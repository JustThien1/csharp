namespace TourGuideHCM.API.Models;

public class DashboardDto
{
    public int OnlineUsers { get; set; } = 0;       // Số user (unique) online trong 2 phút qua
    public int OnlineDevices { get; set; } = 0;     // Số thiết bị (unique DeviceId) online trong 2 phút qua
    public int ListeningUsers { get; set; } = 0;    // Số user đang nghe audio trong 1 phút qua
    public string HotPoiName { get; set; } = "Chưa có dữ liệu";
    public int AvgListenTime { get; set; } = 0;
    public int TotalToday { get; set; } = 0;        // Tổng lượt nghe hôm nay
    public List<ActiveSession> ActiveSessions { get; set; } = new();
    public List<DeviceBreakdown> DeviceBreakdown { get; set; } = new();  // Thống kê theo platform
}

public class DashboardUpdate
{
    public int OnlineUsers { get; set; }
    public int ListeningUsers { get; set; }
    public string HotPoiName { get; set; } = "Chưa có dữ liệu";
    public int AvgListenTime { get; set; }
    public List<ActiveSession> ActiveSessions { get; set; } = new();
}

public class ActiveSession
{
    public string UserName { get; set; } = string.Empty;
    public string CurrentPoi { get; set; } = string.Empty;
    public string ConnectedTime { get; set; } = string.Empty;

    // ====================== MỞ RỘNG ======================
    public string DeviceId { get; set; } = string.Empty;      // rút gọn hiển thị
    public string DeviceName { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string Status { get; set; } = "online";           // online | listening | idle
}

public class DeviceBreakdown
{
    public string Platform { get; set; } = string.Empty;   // "Android", "iOS", "Windows"
    public int Count { get; set; }
}
public class TopPoiItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;     // Loại POI: Di tích, Bảo tàng, Công viên...
    public int Count { get; set; }                      // Số lượt nghe / truy cập
    public double AvgRating { get; set; }                     // Điểm trung bình
    public int TotalTimeListened { get; set; }                // Tổng thời gian nghe (giây)
    public string ImageUrl { get; set; } = string.Empty;      // (optional) ảnh đại diện
}