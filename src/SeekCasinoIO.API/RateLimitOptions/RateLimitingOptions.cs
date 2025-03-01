using AspNetCoreRateLimit;
using Microsoft.Extensions.Options;
using SeekCasinoIO.API.Attributes;

namespace SeekCasinoIO.API.RateLimitOptions;

/// <summary>
/// AspNetCoreRateLimit kütüphanesi için yapılandırma uzantıları
/// </summary>
public static class RateLimitingOptions
{
    /// <summary>
    /// Rate limit özelliklerini yapılandırır.
    /// </summary>
    public static IServiceCollection ConfigureRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        // Load general options from appsettings.json
        services.Configure<IpRateLimitOptions>(configuration.GetSection("IpRateLimiting"));
        services.Configure<IpRateLimitPolicies>(configuration.GetSection("IpRateLimitPolicies"));

        // Client ID tabanlı rate limit için konfigurasyon
        services.Configure<ClientRateLimitOptions>(configuration.GetSection("ClientRateLimiting"));
        services.Configure<ClientRateLimitPolicies>(configuration.GetSection("ClientRateLimitPolicies"));

        // Rate limit deposunu bellek içinde tutma
        services.AddInMemoryRateLimiting();

        // Rate limit için gerekli servislerin eklenmesi
        services.AddSingleton<IRateLimitConfiguration, CustomRateLimitConfiguration>();
        
        // RateLimit counters and rules işleyicileri için bellek önbelleklemesi
        services.AddMemoryCache();

        // Özelleştirilmiş rate limit konfigürasyon yükleyicisi
        services.AddTransient<CustomRateLimitConfigurationLoader>();

        // Uygulama başlangıcında çalıştırılacak attribute rate limit yükleyicisi
        services.AddHostedService<RateLimitAttributeLoader>();

        return services;
    }

    /// <summary>
    /// Rate limit middleware'ini yapılandırır.
    /// </summary>
    public static IApplicationBuilder UseRateLimitingMiddleware(this IApplicationBuilder app)
    {
        // IP rate limit middleware'ini ekle
        app.UseIpRateLimiting();
        
        // Client rate limit middleware'ini ekle
        app.UseClientRateLimiting();

        return app;
    }
}

/// <summary>
/// Uygulama başlangıcında tüm controller'ları tarayarak CustomRateLimit özniteliklerini yükler
/// </summary>
public class RateLimitAttributeLoader : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    
    public RateLimitAttributeLoader(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Servis scope'u oluştur
        using var scope = _serviceProvider.CreateScope();
        
        // CustomRateLimitConfigurationLoader servisini al
        var loader = scope.ServiceProvider.GetRequiredService<CustomRateLimitConfigurationLoader>();
        
        // Tüm uygulamadaki assembly'leri al
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.FullName?.StartsWith("SeekCasinoIO") == true);
        
        // CustomRateLimit özniteliklerini yükle
        loader.LoadRules(assemblies);
        
        return Task.CompletedTask;
    }
    
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}