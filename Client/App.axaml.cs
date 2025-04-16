using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Client.Services;
using Client.Tools;
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

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // //Demo functionality (SHOULD BE REMOVED IN V1 RELEASE)
            // // Set expiration date and time (UTC)
            
            // var expirationDate = new DateTime(2025, 4, 24, 2, 0, 0, DateTimeKind.Utc);
            // var currentDate = DateTime.UtcNow;

            // if (currentDate > expirationDate)
            // {
            //     desktop.MainWindow = new Window { Height = 0, Width = 0 };
            //     await UiTools.ShowMessageAsync("Demo Expired", "This demo version of the Scan Lab - Client app is expired. Please contact the developer to receive the full version", UiTools.MessageType.Error);
            //     Environment.Exit(0);
            //     return;
            // }

            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(ApiService, ScannerService)
            };
        }



        base.OnFrameworkInitializationCompleted();
    }
}
