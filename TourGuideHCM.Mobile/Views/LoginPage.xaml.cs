using System.Text;
using System.Text.Json;

namespace TourGuideHCM.Mobile.Views;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        try
        {
            var client = new HttpClient();

            var data = new
            {
                Username = txtUsername.Text,
                PasswordHash = txtPassword.Text
            };

            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var res = await client.PostAsync("http://localhost:5284/api/auth/login", content);

            if (res.IsSuccessStatusCode)
            {
                await DisplayAlert("OK", "Login success", "OK");

                Application.Current!.MainPage = new NavigationPage(new HomePage());
            }
            else
            {
                await DisplayAlert("Fail", "Login failed", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }
}