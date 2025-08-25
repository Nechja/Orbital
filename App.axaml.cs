using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System;
using System.Linq;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using OrbitalDocking.Services;
using OrbitalDocking.ViewModels;
using OrbitalDocking.Views;

namespace OrbitalDocking;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();
            
            // Configure DI
            var services = new ServiceCollection();
            services.AddOrbitalDockingServices();
            _serviceProvider = services.BuildServiceProvider();
            
            // Set the service provider for use throughout the app
            ServiceLocator.SetServiceProvider(_serviceProvider);
            
            // Resolve MainWindowViewModel from DI
            var mainWindowViewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();
            
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainWindowViewModel,
            };
            
            desktop.Exit += OnExit;
        }

        base.OnFrameworkInitializationCompleted();
    }
    
    private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        _serviceProvider?.Dispose();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}