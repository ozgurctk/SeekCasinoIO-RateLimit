using AspNetCoreRateLimit;
using Microsoft.Extensions.Options;

namespace SeekCasinoIO.API.Middleware;

/// <summary>
/// Özel rate limit kurallarını uygulayan sınıf
/// </summary>
public class CustomRateLimitConfiguration : RateLimitConfiguration
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CustomRateLimitConfiguration(
        IOptions<IpRateLimitOptions> ipOptions,
        IOptions<ClientRateLimitOptions> clientOptions,
        IHttpContextAccessor httpContextAccessor) : base(ipOptions, clientOptions)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// IP adresi bazlı rate limit kurallarını belirler
    /// </summary>
    public override void RegisterResolvers()
    {
        base.RegisterResolvers();

        // Ekstra client id resolver ekle - Bu ileride JWT token içindeki id'ye göre de yapılabilir
        ClientResolvers.Add(new ClientQueryStringResolveContributor(_httpContextAccessor));
    }
}

/// <summary>
/// Query string üzerinden client ID çözümleme (X-API-KEY parametresi için)
/// </summary>
public class ClientQueryStringResolveContributor : IClientResolveContributor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ClientQueryStringResolveContributor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? ResolveClient()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return null;
        
        // X-API-KEY headerından client ID çözümlemesi
        if (httpContext.Request.Headers.TryGetValue("X-API-KEY", out var apiKey))
        {
            return apiKey.ToString();
        }
        
        // Query stringden client ID çözümlemesi
        if (httpContext.Request.Query.TryGetValue("apiKey", out var queryApiKey))
        {
            return queryApiKey.ToString();
        }

        // Giriş yapmış kullanıcı varsa kullanıcı ID'si
        if (httpContext.User?.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "sub");
            if (userIdClaim != null)
            {
                return $"user-{userIdClaim.Value}";
            }
        }

        return null;
    }

    /// <summary>
    /// IClientResolveContributor arabiriminin async metodunu implemente eder
    /// </summary>
    public Task<string?> ResolveClientAsync(HttpContext httpContext)
    {
        // X-API-KEY headerından client ID çözümlemesi
        if (httpContext.Request.Headers.TryGetValue("X-API-KEY", out var apiKey))
        {
            return Task.FromResult<string?>(apiKey.ToString());
        }
        
        // Query stringden client ID çözümlemesi
        if (httpContext.Request.Query.TryGetValue("apiKey", out var queryApiKey))
        {
            return Task.FromResult<string?>(queryApiKey.ToString());
        }

        // Giriş yapmış kullanıcı varsa kullanıcı ID'si
        if (httpContext.User?.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "sub");
            if (userIdClaim != null)
            {
                return Task.FromResult<string?>($"user-{userIdClaim.Value}");
            }
        }

        return Task.FromResult<string?>(null);
    }
}