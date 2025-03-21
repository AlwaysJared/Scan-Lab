using Client.Interfaces;
using Client.Pages;
using Client.Platforms;
using Client.ViewModels;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.LifecycleEvents;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Windows.Graphics;
using WinRT.Interop;

namespace Client;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();

		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit() // Add this line
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		builder.Services.AddSingleton<ScannerService>();

		// Register pages for DI
		builder.Services.AddTransient<DashboardPage>();
		builder.Services.AddTransient<OrderFormPage>();
		builder.Services.AddTransient<SettingsPage>();
		builder.Services.AddTransient<SettingsViewModel>();

		builder.ConfigureLifecycleEvents(events =>
            {
#if WINDOWS
                events.AddWindows(windows =>
                {
                    windows.OnWindowCreated(window =>
                    {
                        var nativeWindow = (Microsoft.UI.Xaml.Window)window;

                        // Get the window handle (HWND)
                        nint windowHandle = WindowNative.GetWindowHandle(nativeWindow);
                        var windowId = Win32Interop.GetWindowIdFromWindow(windowHandle);
                        var appWindow = AppWindow.GetFromWindowId(windowId);

                        if (appWindow != null)
                        {
                            // Ensure the window is using OverlappedPresenter
                            if (appWindow.Presenter is OverlappedPresenter presenter)
                            {
                                // Allow resizing
                                presenter.IsResizable = true;
                                presenter.IsMaximizable = true;
                            }

                            // Set the minimum window size
                            appWindow.Resize(new SizeInt32(800, 500)); // Minimum width: 400px, Minimum height: 300px
                        }
                    });
                });
#endif
            });

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
