using Client.Pages;
using Microsoft.Extensions.Logging;

namespace Client;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();

		builder
			.UseMauiApp<App>()
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

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
