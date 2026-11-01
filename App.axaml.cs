using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
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
            var services = new ServiceCollection();
            services.AddOrbitalDockingServices();
            _serviceProvider = services.BuildServiceProvider();

            var mainWindow = new MainWindow
            {
                DataContext = _serviceProvider.GetRequiredService<MainWindowViewModel>(),
            };
            desktop.MainWindow = mainWindow;

            // Closing the window hides it to the tray; Exit from the tray menu shuts down.
            var tray = _serviceProvider.GetRequiredService<ITrayService>();
            tray.Initialize();
            tray.ExitRequested += (_, _) => desktop.Shutdown();
            mainWindow.Closing += (_, e) =>
            {
                e.Cancel = true;
                mainWindow.Hide();
            };

            desktop.Exit += (_, _) =>
            {
                (mainWindow.DataContext as MainWindowViewModel)?.Dispose();
                _serviceProvider.Dispose();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
