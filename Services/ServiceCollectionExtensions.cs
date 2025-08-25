using System;
using Microsoft.Extensions.DependencyInjection;
using Docker.DotNet;
using OrbitalDocking.ViewModels;

namespace OrbitalDocking.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOrbitalDockingServices(this IServiceCollection services)
    {
        // Docker services
        services.AddSingleton<IDockerClientFactory, DockerClientFactory>();
        services.AddSingleton<DockerClient>(provider => 
            provider.GetRequiredService<IDockerClientFactory>().CreateClient());
        services.AddSingleton<IDockerMapper, DockerMapper>();
        services.AddSingleton<IDockerService, DockerService>();
        
        // Other services
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<IDialogService, DialogService>();
        
        // ViewModels
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<ContainerViewModel>();
        
        // Factory for LogsViewModel since it needs runtime parameters
        services.AddTransient<Func<string, string, LogsViewModel>>(provider => 
            (containerId, containerName) => 
                new LogsViewModel(
                    containerId, 
                    containerName, 
                    provider.GetRequiredService<DockerClient>()));
        
        return services;
    }
}