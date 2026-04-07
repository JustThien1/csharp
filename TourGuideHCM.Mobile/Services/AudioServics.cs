namespace TourGuideHCM.Mobile.Services;

public class AudioService
{
    public async Task PlayAsync(string url)
    {
        try
        {
            await Launcher.OpenAsync(url);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Audio error: " + ex.Message);
        }
    }
}