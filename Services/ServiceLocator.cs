using System;
using Microsoft.Extensions.DependencyInjection;

namespace OrbitalDocking.Services;

public static class ServiceLocator
{
    private static IServiceProvider? _serviceProvider;
    
    public static void SetServiceProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public static T GetService<T>() where T : notnull
    {
        if (_serviceProvider == null)
            throw new InvalidOperationException("ServiceProvider not initialized");
            
        return _serviceProvider.GetRequiredService<T>();
    }
}