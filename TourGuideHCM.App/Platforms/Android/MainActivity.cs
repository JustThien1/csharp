using Android.App;
using Android.Content.PM;
using Android.OS;

namespace TourGuideHCM.App
{
    [Activity(Theme = "@style/Maui.SplashTheme",
              MainLauncher = true,
              ConfigurationChanges = ConfigChanges.ScreenSize
                                  | ConfigChanges.Orientation
                                  | ConfigChanges.UiMode
                                  | ConfigChanges.ScreenLayout
                                  | ConfigChanges.SmallestScreenSize
                                  | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // ✅ CẤU HÌNH WEBVIEW CHO ANDROID - Cho phép load local files + mixed content
            var webView = new Android.Webkit.WebView(this);
            if (webView.Settings != null)
            {
                webView.Settings.JavaScriptEnabled = true;
                webView.Settings.DomStorageEnabled = true;
                webView.Settings.AllowFileAccess = true;
                webView.Settings.AllowFileAccessFromFileURLs = true;
                webView.Settings.AllowUniversalAccessFromFileURLs = true;
                webView.Settings.MixedContentMode = Android.Webkit.MixedContentHandling.AlwaysAllow;
                webView.Settings.LoadsImagesAutomatically = true;
            }
        }
    }
}