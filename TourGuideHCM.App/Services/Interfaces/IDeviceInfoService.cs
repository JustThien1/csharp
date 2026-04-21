namespace TourGuideHCM.App.Services.Interfaces;

/// <summary>
/// Cung cấp thông tin thiết bị cho việc monitoring & user tracking.
/// DeviceId được sinh 1 lần duy nhất per thiết bị và lưu trong Preferences
/// → mỗi lần cài lại app sẽ sinh DeviceId mới (đây là hành vi mong muốn).
/// </summary>
public interface IDeviceInfoService
{
    /// <summary>ID duy nhất của thiết bị (GUID 32 ký tự, lưu trong Preferences).</summary>
    string DeviceId { get; }

    /// <summary>Tên thiết bị dạng "Samsung SM-G998B" hoặc "Apple iPhone14,3".</summary>
    string DeviceName { get; }

    /// <summary>"Android", "iOS", "Windows", "MacCatalyst" hoặc "Unknown".</summary>
    string Platform { get; }

    /// <summary>ID user hiện tại đang đăng nhập (0 = chưa đăng nhập).</summary>
    int CurrentUserId { get; }

    /// <summary>Lưu user ID hiện tại vào Preferences (gọi sau khi login thành công).</summary>
    void SetCurrentUser(int userId);
}