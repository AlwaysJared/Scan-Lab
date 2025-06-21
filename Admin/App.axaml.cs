using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using Admin.ViewModels;
using Admin.Views;
using Admin.Services;
using System;
using Avalonia.Controls;
using Admin.Tools;

namespace Admin;

public partial class App : Application
{
    public static ApiService ApiService { get; } = new(); // ✅ Global instance

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            //Demo functionality (SHOULD BE REMOVED IN V1 RELEASE)
            // Set expiration date and time (UTC)

            var expirationDate = new DateTime(2025, 6, 30, 2, 0, 0, DateTimeKind.Utc);
            var currentDate = DateTime.UtcNow;

            if (currentDate > expirationDate)
            {
                desktop.MainWindow = new Window { Height = 0, Width = 0 };
                await UiTools.ShowMessageAsync("Demo Expired", "This demo version of the Scan Lab - Admin app is expired. Please contact the developer to receive the full version", UiTools.MessageType.Error);
                Environment.Exit(0);
                return;
            }

            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            // desktop.MainWindow = new MainWindow
            // {
            //     DataContext = new MainWindowViewModel(),
            // };
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(ApiService)
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}