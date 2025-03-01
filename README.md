# SeekCasinoIO.RateLimit - API Rate Limit Entegrasyonu

Bu repo, SeekCasinoIO projesine API Rate Limit entegrasyonu sağlayan kodları içerir. [AspNetCoreRateLimit](https://github.com/stefanprodan/AspNetCoreRateLimit) kütüphanesini kullanarak hem IP tabanlı hem de client tabanlı rate limit uygular.

## Özellikler

- **IP Bazlı Rate Limit**: İstemcilerin IP adresine göre istek limitleme
- **Client ID Bazlı Rate Limit**: API key veya kullanıcı ID'sine göre istek limitleme
- **Farklı Zaman Aralıkları**: Saniye, dakika, saat ve gün bazında limit tanımlama
- **Endpoint Bazlı Limitleme**: Belirli endpoint'ler için özel limitler tanımlama
- **Whitelist Desteği**: Belirli IP'ler veya endpoint'ler için limit bypass etme
- **Özelleştirilmiş Yanıtlar**: Limit aşımında detaylı JSON yanıtlar

## Kurulum

1. AspNetCoreRateLimit paketini projeye ekleyin:
   ```
   dotnet add package AspNetCoreRateLimit
   ```

2. Bu repodaki RateLimit sınıflarını kendi projenize ekleyin:
   - `RateLimitOptions` klasörü
   - `Middleware` klasöründeki ilgili sınıflar

3. `Program.cs` dosyanızı güncelleyin:
   ```csharp
   // Rate limit için HttpContext accessor
   builder.Services.AddHttpContextAccessor();
   
   // Rate limit konfigürasyonu
   builder.Services.ConfigureRateLimiting(builder.Configuration);
   
   // Custom Rate Limit Configuration
   builder.Services.AddSingleton<IRateLimitConfiguration, CustomRateLimitConfiguration>();
   
   // ...
   
   // Middleware pipeline içinde rate limit kullanımı
   app.UseRateLimitingMiddleware();
   app.UseMiddleware<RateLimitMiddleware>();
   ```

4. `appsettings.json` dosyasına RateLimit konfigürasyonunu ekleyin.

## Konfigürasyon

`appsettings.json` dosyasında aşağıdaki ayarları özelleştirebilirsiniz:

### IP Rate Limit Ayarları

```json
"IpRateLimiting": {
  "EnableEndpointRateLimiting": true,
  "StackBlockedRequests": true,
  "RealIpHeader": "X-Real-IP",
  "ClientIdHeader": "X-ClientId",
  "HttpStatusCode": 429,
  "GeneralRules": [
    {
      "Endpoint": "*",
      "Period": "1s",
      "Limit": 10
    },
    {
      "Endpoint": "*",
      "Period": "1m",
      "Limit": 60
    }
  ]
}
```

### Client Rate Limit Ayarları

```json
"ClientRateLimiting": {
  "EnableEndpointRateLimiting": true,
  "ClientIdHeader": "X-API-KEY",
  "HttpStatusCode": 429,
  "EndpointWhitelist": ["get:/api/health"],
  "GeneralRules": [
    {
      "Endpoint": "*",
      "Period": "1s",
      "Limit": 5
    }
  ]
}
```

## Farklı Müşteri Tipleri İçin Limit Tanımlama

```json
"ClientRateLimitPolicies": {
  "ClientRules": [
    {
      "ClientId": "premium-client",
      "Rules": [
        {
          "Endpoint": "*",
          "Period": "1s",
          "Limit": 20
        }
      ]
    },
    {
      "ClientId": "demo-client",
      "Rules": [
        {
          "Endpoint": "*",
          "Period": "1s",
          "Limit": 2
        }
      ]
    }
  ]
}
```

## Nasıl Çalışır?

1. Her gelen istek için, istemcinin IP adresi veya API anahtarı alınır.
2. Bu bilgiler kullanılarak istemcinin istek sayısı kontrol edilir.
3. Eğer limit aşılmışsa, HTTP 429 (Too Many Requests) yanıtı döndürülür.
4. Limitler içerisindeyse, istek normal şekilde işlenir.

## Daha Fazla Özelleştirme

Daha fazla özelleştirme için `CustomRateLimitConfiguration.cs` sınıfını inceleyebilirsiniz. Bu sınıf, JWT token, query string veya header'daki bilgilere göre client ID'yi tanımlayabilir.

---

## Lisans

MIT
