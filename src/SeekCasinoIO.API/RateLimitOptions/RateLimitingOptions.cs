using AspNetCoreRateLimit;
using Microsoft.Extensions.Options;

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
        services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
        
        // RateLimit counters and rules işleyicileri için bellek önbelleklemesi
        services.AddMemoryCache();

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