namespace TourGuideHCM.App;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public App(IServiceProvider serviceProvider)
    {
        Services = serviceProvider;
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var appShell = Services.GetRequiredService<AppShell>();
        return new Window(appShell);
    }
}
