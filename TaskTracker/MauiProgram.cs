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
		builder.Services.AddSingleton<IBackupFileSystem, SystemBackupFileSystem>();
		builder.Services.AddSingleton<IBackupClock, SystemBackupClock>();
		builder.Services.AddSingleton<IBackupInteraction, MauiBackupInteraction>();
		builder.Services.AddSingleton<BackupService>(serviceProvider =>
			new BackupService(
				serviceProvider.GetRequiredService<TaskRepository>().DatabasePath,
				serviceProvider.GetRequiredService<IBackupFileSystem>(),
				serviceProvider.GetRequiredService<IBackupClock>(),
				serviceProvider.GetRequiredService<IBackupInteraction>()));

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
