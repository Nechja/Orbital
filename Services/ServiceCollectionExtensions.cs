using Docker.DotNet;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OrbitalDocking.ViewModels;
using OrbitalDocking.ViewModels.Pages;

namespace OrbitalDocking.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOrbitalDockingServices(this IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.AddConsole()
                   .AddDebug()
                   .SetMinimumLevel(LogLevel.Information);
        });

        // Docker
        services.AddSingleton<IDockerClientFactory, DockerClientFactory>();
        services.AddSingleton<DockerClient>(provider =>
            provider.GetRequiredService<IDockerClientFactory>().CreateClient());
        services.AddSingleton<IDockerMapper, DockerMapper>();
        services.AddSingleton<IDockerService, DockerService>();

        // UI services
        services.AddSingleton<IUiDispatcher, AvaloniaUiDispatcher>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<ITrayService, TrayService>();
        services.AddSingleton<AppState>();

        // Pages and shell (one window, so one instance of each)
        services.AddSingleton<ContainersPageViewModel>();
        services.AddSingleton<ImagesPageViewModel>();
        services.AddSingleton<VolumesPageViewModel>();
        services.AddSingleton<NetworksPageViewModel>();
        services.AddSingleton<LogsPageViewModel>();
        services.AddSingleton<SettingsPageViewModel>();
        services.AddSingleton<MainWindowViewModel>();

        return services;
    }
}
