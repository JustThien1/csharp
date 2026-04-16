namespace TourGuideHCM.App.Helpers;

public static class DeviceHelper
{
    // IP LAN của máy tính chạy backend
    // ⚠️ Đổi thành IP thực của máy bạn (chạy `ipconfig` trên Windows)
    public const string LanIp = "192.168.0.6";
    public const int Port = 8080;

    public static string GetBaseUrl()
    {
#if ANDROID
        return IsAndroidEmulator()
            ? $"http://10.0.2.2:{Port}"       // Android Emulator (Pixel)
            : $"http://{LanIp}:{Port}";        // Android thật
#elif IOS
        return IsIosSimulator()
            ? $"http://localhost:{Port}"        // iOS Simulator
            : $"http://{LanIp}:{Port}";        // iOS thật
#else
        return $"http://{LanIp}:{Port}";
#endif
    }

#if ANDROID
    private static bool IsAndroidEmulator()
    {
        var fingerprint = Android.OS.Build.Fingerprint ?? "";
        var model = Android.OS.Build.Model ?? "";
        var hardware = Android.OS.Build.Hardware ?? "";
        var product = Android.OS.Build.Product ?? "";

        return fingerprint.StartsWith("generic", StringComparison.OrdinalIgnoreCase)
            || fingerprint.StartsWith("unknown", StringComparison.OrdinalIgnoreCase)
            || fingerprint.Contains("emulator", StringComparison.OrdinalIgnoreCase)
            || model.Contains("Emulator", StringComparison.OrdinalIgnoreCase)
            || model.Contains("Android SDK", StringComparison.OrdinalIgnoreCase)
            || hardware.Contains("goldfish", StringComparison.OrdinalIgnoreCase)
            || hardware.Contains("ranchu", StringComparison.OrdinalIgnoreCase)
            || product.Contains("sdk", StringComparison.OrdinalIgnoreCase)
            || product.Contains("emulator", StringComparison.OrdinalIgnoreCase);
    }
#endif

#if IOS
    private static bool IsIosSimulator() =>
        ObjCRuntime.Runtime.Arch == ObjCRuntime.Arch.SIMULATOR;
#endif
}