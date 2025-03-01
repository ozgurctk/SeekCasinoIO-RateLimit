using Microsoft.AspNetCore.Mvc;
using SeekCasinoIO.API.Attributes;

namespace SeekCasinoIO.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[CustomRateLimit(100, "1m", "standard")] // Controller seviyesinde rate limit - tüm metotlar için geçerli
public class CasinosRateLimitController : ControllerBase
{
    private readonly ILogger<CasinosRateLimitController> _logger;

    public CasinosRateLimitController(ILogger<CasinosRateLimitController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Tüm casinoları listeler - Controller seviyesindeki limiti kullanır
    /// </summary>
    [HttpGet]
    public IActionResult GetAll()
    {
        _logger.LogInformation("Casino listesi çağrıldı");
        
        return Ok(new
        {
            message = "Casino listesi başarıyla döndürüldü",
            limit = "Dakikada 100 istek",
            count = 5, // Örnek veri
            casinos = new[] 
            { 
                new { id = Guid.NewGuid(), name = "Casino 1", rating = 4.5 },
                new { id = Guid.NewGuid(), name = "Casino 2", rating = 4.7 },
                new { id = Guid.NewGuid(), name = "Casino 3", rating = 4.2 },
                new { id = Guid.NewGuid(), name = "Casino 4", rating = 4.8 },
                new { id = Guid.NewGuid(), name = "Casino 5", rating = 3.9 }
            }
        });
    }

    /// <summary>
    /// Casino detayları - Özel dakikada 10 istek limiti uygulanır
    /// </summary>
    [HttpGet("{id:guid}")]
    [CustomRateLimit(10, "1m", "standard")] // Metot seviyesinde rate limit - sadece bu metot için geçerli
    public IActionResult GetById(Guid id)
    {
        _logger.LogInformation("Casino detayı çağrıldı. ID: {Id}", id);
        
        return Ok(new
        {
            message = "Casino detayları başarıyla döndürüldü",
            limit = "Dakikada 10 istek (detaylar için daha düşük limit)",
            casino = new
            {
                id = id,
                name = "Test Casino",
                rating = 4.5,
                description = "Bu bir test casino açıklamasıdır.",
                games = new[] { "Slot", "Poker", "Blackjack", "Rulet" },
                bonuses = new[] 
                { 
                    new { type = "Welcome", amount = "$100" },
                    new { type = "Free Spins", amount = "50 Free Spins" } 
                }
            }
        });
    }

    /// <summary>
    /// Casino oluşturma - Saat başına 5 istek ile daha sıkı limit
    /// </summary>
    [HttpPost]
    [CustomRateLimit(5, "1h", "standard")] // Saat başına 5 istek limiti
    [CustomRateLimit(50, "1h", "premium")] // Premium müşteriler için saat başına 50 istek
    public IActionResult Create()
    {
        _logger.LogInformation("Yeni casino oluşturma isteği alındı");
        
        return Ok(new
        {
            message = "Casino başarıyla oluşturuldu",
            limit = "Saatte 5 istek (kaynak tüketen bir işlem için daha sıkı limit)",
            id = Guid.NewGuid(),
            createdAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Casino güncelleme - Dakikada 15 istek limiti
    /// </summary>
    [HttpPut("{id:guid}")]
    [CustomRateLimit(15, "1m", "standard")]
    public IActionResult Update(Guid id)
    {
        _logger.LogInformation("Casino güncelleme isteği alındı. ID: {Id}", id);
        
        return Ok(new
        {
            message = "Casino başarıyla güncellendi",
            limit = "Dakikada 15 istek",
            id = id,
            updatedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Casino silme - Günlük 20 istek limiti
    /// </summary>
    [HttpDelete("{id:guid}")]
    [CustomRateLimit(20, "1d", "standard")] 
    public IActionResult Delete(Guid id)
    {
        _logger.LogInformation("Casino silme isteği alındı. ID: {Id}", id);
        
        return Ok(new
        {
            message = "Casino başarıyla silindi",
            limit = "Günde 20 istek (silme işlemleri için daha sıkı günlük limit)",
            id = id,
            deletedAt = DateTime.UtcNow
        });
    }
}