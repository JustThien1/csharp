using Microsoft.Maui.Storage;
using TourGuideHCM.App.Views;

namespace TourGuideHCM.App
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // 👋 Hiển thị username (nếu có)
            var username = Preferences.Get("username", "");
            if (!string.IsNullOrEmpty(username))
            {
                this.Title = "Xin chào " + username;
            }
        }

        private void OnLogoutClicked(object sender, EventArgs e)
        {
            // ❌ Xóa trạng thái đăng nhập
            Preferences.Remove("username");

            // 🔄 Quay về login
            Application.Current.MainPage = new NavigationPage(
                App.Services.GetService<LoginPage>()
            );
        }
    }
}