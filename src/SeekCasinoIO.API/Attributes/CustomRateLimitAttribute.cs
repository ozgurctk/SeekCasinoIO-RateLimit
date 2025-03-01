using AspNetCoreRateLimit;
using Microsoft.Extensions.Options;

namespace SeekCasinoIO.API.Attributes;

/// <summary>
/// Metod veya sınıf seviyesinde rate limit kuralları eklemeyi sağlayan öznitelik
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class CustomRateLimitAttribute : Attribute
{
    /// <summary>
    /// Belirli bir zaman diliminde izin verilen maksimum istek sayısı
    /// </summary>
    public int Limit { get; set; }

    /// <summary>
    /// Limit için zaman dilimi: s, m, h, d (saniye, dakika, saat, gün)
    /// </summary>
    public string Period { get; set; }

    /// <summary>
    /// Rate limit uygulanacak müşteri tipi (standart, premium, admin vb)
    /// </summary>
    public string ClientType { get; set; }

    public CustomRateLimitAttribute(int limit, string period, string clientType = "standard")
    {
        Limit = limit;
        Period = period;
        ClientType = clientType;
    }
}

/// <summary>
/// CustomRateLimitAttribute özniteliklerini işleyen sınıf
/// </summary>
public class CustomRateLimitConfigurationLoader
{
    private readonly IpRateLimitOptions _ipOptions;
    private readonly ClientRateLimitOptions _clientOptions;

    public CustomRateLimitConfigurationLoader(
        IOptions<IpRateLimitOptions> ipOptions,
        IOptions<ClientRateLimitOptions> clientOptions)
    {
        _ipOptions = ipOptions.Value;
        _clientOptions = clientOptions.Value;
    }

    /// <summary>
    /// Controller'da tanımlanan rate limit kurallarını yükler
    /// </summary>
    public void LoadRules(IEnumerable<System.Reflection.Assembly> assemblies)
    {
        var controllers = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract && 
                        t.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase));

        foreach (var controller in controllers)
        {
            // Controller üzerindeki tüm CustomRateLimitAttribute'leri topla
            var controllerAttributes = controller.GetCustomAttributes(typeof(CustomRateLimitAttribute), true)
                .Cast<CustomRateLimitAttribute>();

            // Controller metodlarını incele
            var methods = controller.GetMethods();
            foreach (var method in methods)
            {
                var methodAttributes = method.GetCustomAttributes(typeof(CustomRateLimitAttribute), true)
                    .Cast<CustomRateLimitAttribute>();
                
                // Hem controller hem de metod seviyesindeki attribute'leri birleştir
                var attributes = controllerAttributes.Concat(methodAttributes);
                
                foreach (var attribute in attributes)
                {
                    // Endpoint path'ini oluştur
                    var endpoint = $"*/{controller.Name.Replace("Controller", "")}";
                    if (methodAttributes.Contains(attribute))
                    {
                        endpoint = $"*/{controller.Name.Replace("Controller", "")}/{method.Name}";
                    }

                    // IP Rate limit kuralını ekle
                    var ipRule = new RateLimitRule
                    {
                        Endpoint = endpoint,
                        Limit = attribute.Limit,
                        Period = attribute.Period
                    };
                    _ipOptions.GeneralRules.Add(ipRule);

                    // Client Rate limit kuralını ekle
                    var clientRule = new RateLimitRule
                    {
                        Endpoint = endpoint,
                        Limit = attribute.Limit,
                        Period = attribute.Period
                    };
                    _clientOptions.GeneralRules.Add(clientRule);

                    // Farklı client tipleri için özel kurallar
                    if (attribute.ClientType == "premium")
                    {
                        // Premium müşteriler için limiti 5 kat yükselt
                        var premiumRule = new RateLimitRule
                        {
                            Endpoint = endpoint,
                            Limit = attribute.Limit * 5,
                            Period = attribute.Period
                        };
                        
                        // Bu kuralı ClientRules'a ekle
                        var premiumClientRule = new ClientRateLimitPolicy
                        {
                            ClientId = "premium-client",
                            Rules = new List<RateLimitRule> { premiumRule }
                        };
                        
                        // Bu kısmı appsettings.json'daki tanımlamalara ekleyerek de yapabilirsiniz
                    }
                }
            }
        }
    }
}