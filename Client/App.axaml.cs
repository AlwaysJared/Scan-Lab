using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Client.Services;
using Client.ViewModels;
using Client.Views;

namespace Client;

public class App : Application
{
    public static ScannerService ScannerService { get; } = new(); // ✅ Global instance
    public static ApiService ApiService { get; } = new(); // ✅ Global instance

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(ApiService,ScannerService)
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
