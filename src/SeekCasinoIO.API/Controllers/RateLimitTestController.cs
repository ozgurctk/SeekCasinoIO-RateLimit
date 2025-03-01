using Microsoft.AspNetCore.Mvc;

namespace SeekCasinoIO.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RateLimitTestController : ControllerBase
{
    private readonly ILogger<RateLimitTestController> _logger;

    public RateLimitTestController(ILogger<RateLimitTestController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Rate limit test endpoint - Genel limit kurallarına tabidir
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        _logger.LogInformation("RateLimitTest Get metodu çağrıldı. IP: {IP}", HttpContext.Connection.RemoteIpAddress);
        
        return Ok(new
        {
            message = "RateLimit Test başarılı!",
            ip = HttpContext.Connection.RemoteIpAddress?.ToString(),
            timestamp = DateTime.UtcNow,
            clientId = HttpContext.Request.Headers.TryGetValue("X-API-KEY", out var apiKey) ? apiKey.ToString() : "Belirsiz"
        });
    }

    /// <summary>
    /// Bu endpoint 5 saniyede 2 istekle sınırlandırılmıştır
    /// </summary>
    [HttpGet("limited")]
    public IActionResult GetLimited()
    {
        _logger.LogInformation("RateLimitTest GetLimited metodu çağrıldı. IP: {IP}", HttpContext.Connection.RemoteIpAddress);
        
        // Bu header ile client'a ne kadar istek yapabileceğini bildiriyoruz
        Response.Headers.Add("X-Rate-Limit-Limit", "2");
        Response.Headers.Add("X-Rate-Limit-Remaining", "1"); // Gerçek projede bu değer hesaplanmalı
        Response.Headers.Add("X-Rate-Limit-Period", "5s");
        
        return Ok(new
        {
            message = "Sınırlı RateLimit Test başarılı!",
            ip = HttpContext.Connection.RemoteIpAddress?.ToString(),
            timestamp = DateTime.UtcNow,
            rateLimit = "5 saniyede 2 istek"
        });
    }

    /// <summary>
    /// Admin yetkisi gerektiren ve daha yüksek rate limit'e sahip olan endpoint
    /// </summary>
    [HttpGet("admin")]
    public IActionResult GetAdmin()
    {
        if (!HttpContext.Request.Headers.TryGetValue("X-API-KEY", out var apiKey) || apiKey != "admin-api")
        {
            return Unauthorized(new { message = "Bu endpoint için admin API key gerekli!" });
        }
        
        _logger.LogInformation("RateLimitTest GetAdmin metodu çağrıldı. API Key: {ApiKey}", apiKey);
        
        return Ok(new
        {
            message = "Admin RateLimit Test başarılı!",
            ip = HttpContext.Connection.RemoteIpAddress?.ToString(),
            timestamp = DateTime.UtcNow,
            apiKey = apiKey.ToString(),
            rateLimit = "Yüksek limitli admin endpoint"
        });
    }
}