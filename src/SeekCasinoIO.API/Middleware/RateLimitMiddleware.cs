using System.Net;
using AspNetCoreRateLimit;
using Microsoft.Extensions.Options;

namespace SeekCasinoIO.API.Middleware;

/// <summary>
/// Rate limit tarafından engellenen isteklerin daha açıklayıcı yanıtlar döndürmesini sağlayan middleware
/// </summary>
public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitMiddleware> _logger;
    private readonly IpRateLimitOptions _ipRateLimitOptions;
    private readonly ClientRateLimitOptions _clientRateLimitOptions;

    public RateLimitMiddleware(
        RequestDelegate next,
        ILogger<RateLimitMiddleware> logger,
        IOptions<IpRateLimitOptions> ipRateLimitOptions,
        IOptions<ClientRateLimitOptions> clientRateLimitOptions)
    {
        _next = next;
        _logger = logger;
        _ipRateLimitOptions = ipRateLimitOptions.Value;
        _clientRateLimitOptions = clientRateLimitOptions.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);

            // Rate limit headers ekle
            if (context.Response.StatusCode == (int)HttpStatusCode.TooManyRequests)
            {
                _logger.LogWarning("Rate limit aşıldı. IP: {IpAddress}, Path: {Path}, Method: {Method}",
                    context.Connection.RemoteIpAddress,
                    context.Request.Path,
                    context.Request.Method);

                // Limit aşıldığında daha açıklayıcı bir mesaj ekle
                context.Response.ContentType = "application/json";
                var limitType = context.Response.Headers.ContainsKey("X-ClientRateLimit-Limit") 
                    ? "client" 
                    : "IP";

                var retryAfter = context.Response.Headers["Retry-After"];
                var message = new
                {
                    status = 429,
                    title = "Çok fazla istek",
                    detail = $"API istekleri için {limitType} tabanlı rate limit aşıldı. Lütfen {retryAfter} saniye sonra tekrar deneyin.",
                    retryAfter = retryAfter
                };

                await context.Response.WriteAsJsonAsync(message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rate limit middleware içinde hata oluştu");
            throw;
        }
    }

    /// <summary>
    /// Rate limit aşıldığında ilgili headerları ekler
    /// </summary>
    private void AddRateLimitHeaders(HttpContext context)
    {
        if (_ipRateLimitOptions.GeneralRules.Count > 0)
        {
            context.Response.Headers["X-Rate-Limit-Limit"] = _ipRateLimitOptions.GeneralRules[0].Limit;
            
            var period = _ipRateLimitOptions.GeneralRules[0].Period;
            var periodTimespan = period.ToTimeSpan();
            context.Response.Headers["X-Rate-Limit-Period"] = $"{periodTimespan.TotalSeconds} saniye";
        }
    }
}