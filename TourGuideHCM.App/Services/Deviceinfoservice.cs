using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
using TourGuideHCM.App.Services.Interfaces;

namespace TourGuideHCM.App.Services;

/// <summary>
/// Implementation của <see cref="IDeviceInfoService"/>.
/// - <c>DeviceId</c>: sinh GUID 1 lần duy nhất, lưu Preferences (reset khi gỡ/cài lại app).
/// - <c>DeviceName</c>, <c>Platform</c>: lấy từ <see cref="DeviceInfo.Current"/>, có cache in-memory.
/// - <c>CurrentUserId</c>: đọc từ Preferences, 0 nếu chưa login.
/// </summary>
public class DeviceInfoService : IDeviceInfoService
{
    private const string DeviceIdKey = "tourguide_device_id";
    private const string UserIdKey = "tourguide_current_user_id";

    private string? _deviceId;
    private string? _deviceName;
    private string? _platform;

    public string DeviceId
    {
        get
        {
            if (!string.IsNullOrEmpty(_deviceId)) return _deviceId;

            _deviceId = Preferences.Default.Get(DeviceIdKey, string.Empty);
            if (string.IsNullOrEmpty(_deviceId))
            {
                _deviceId = Guid.NewGuid().ToString("N");   // 32 chars, no hyphens
                Preferences.Default.Set(DeviceIdKey, _deviceId);
            }
            return _deviceId;
        }
    }

    public string DeviceName
    {
        get
        {
            if (!string.IsNullOrEmpty(_deviceName)) return _deviceName;

            try
            {
                var manufacturer = DeviceInfo.Current.Manufacturer ?? "";
                var model = DeviceInfo.Current.Model ?? "Unknown";
                _deviceName = string.IsNullOrEmpty(manufacturer)
                    ? model
                    : $"{manufacturer} {model}".Trim();
            }
            catch
            {
                _deviceName = "Unknown Device";
            }
            return _deviceName;
        }
    }

    public string Platform
    {
        get
        {
            if (!string.IsNullOrEmpty(_platform)) return _platform;
            try
            {
                _platform = DeviceInfo.Current.Platform.ToString();  // Android / iOS / WinUI
                if (_platform == "WinUI") _platform = "Windows";
            }
            catch
            {
                _platform = "Unknown";
            }
            return _platform;
        }
    }

    public int CurrentUserId => Preferences.Default.Get(UserIdKey, 0);

    public void SetCurrentUser(int userId)
    {
        Preferences.Default.Set(UserIdKey, userId);
    }
}