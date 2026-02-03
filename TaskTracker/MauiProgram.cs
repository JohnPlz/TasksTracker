using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using TaskTracker.Services;

namespace TaskTracker;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		builder.Services.AddSingleton<TaskRepository>();
		builder.Services.AddSingleton<MeterRepository>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
