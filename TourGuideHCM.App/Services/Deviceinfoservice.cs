using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;

namespace TourGuideHCM.App.Services;

/// <summary>
/// Cung cấp thông tin thiết bị cho việc monitoring.
/// DeviceId được sinh 1 lần duy nhất per thiết bị và lưu trong Preferences
/// → mỗi lần cài lại app sẽ sinh DeviceId mới (đây là hành vi mong muốn).
/// </summary>
public interface IDeviceInfoService
{
    string DeviceId { get; }
    string DeviceName { get; }
    string Platform { get; }
    int CurrentUserId { get; }
    void SetCurrentUser(int userId);
}

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

    public int CurrentUserId
    {
        get => Preferences.Default.Get(UserIdKey, 0);
    }

    public void SetCurrentUser(int userId)
    {
        Preferences.Default.Set(UserIdKey, userId);
    }
}