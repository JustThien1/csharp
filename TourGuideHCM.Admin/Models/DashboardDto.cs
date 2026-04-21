namespace TourGuideHCM.Admin.Models;

// Model cho Dashboard Analytics (trang chính)
public class DashboardDto
{
    public int TotalPoi { get; set; } = 0;
    public int TotalUsers { get; set; } = 0;        // Giữ lại cho tương thích (= TotalDevices)
    public int TotalDevices { get; set; } = 0;      // MỚI: số thiết bị khác nhau đã truy cập
    public string TopPoi { get; set; } = "Chưa có dữ liệu";
    public int AvgTime { get; set; } = 0;
    public List<TopPoiItem> TopPois { get; set; } = new();
    public int[] DailyViews { get; set; } = new int[7];
    public List<DeviceBreakdown> PlatformBreakdown { get; set; } = new();   // MỚI
}

public class TopPoiItem
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; } = 0;
}

// ====================== Model cho Monitoring Realtime (ĐÃ MỞ RỘNG) ======================
public class RealtimeDashboardDto
{
    public int OnlineUsers { get; set; } = 0;
    public int OnlineDevices { get; set; } = 0;
    public int ListeningUsers { get; set; } = 0;
    public string HotPoiName { get; set; } = "Chưa có dữ liệu";
    public int AvgListenTime { get; set; } = 0;
    public int TotalToday { get; set; } = 0;
    public List<ActiveSession> ActiveSessions { get; set; } = new();
    public List<DeviceBreakdown> DeviceBreakdown { get; set; } = new();
}

public class ActiveSession
{
    public string UserName { get; set; } = string.Empty;
    public string CurrentPoi { get; set; } = string.Empty;
    public string ConnectedTime { get; set; } = string.Empty;

    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string Status { get; set; } = "online";    // online | listening | idle
}

public class DeviceBreakdown
{
    public string Platform { get; set; } = string.Empty;
    public int Count { get; set; }
}

// Model cho SignalR (giữ lại để tránh break code cũ)
public class DashboardUpdate
{
    public int OnlineUsers { get; set; }
    public int ListeningUsers { get; set; }
    public string HotPoiName { get; set; } = "Chưa có dữ liệu";
    public int AvgListenTime { get; set; }
    public List<ActiveSession> ActiveSessions { get; set; } = new();
}
