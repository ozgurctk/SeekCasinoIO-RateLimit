using AspNetCoreRateLimit;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.OpenApi.Models;
using SeekCasinoIO.API.Middleware;
using SeekCasinoIO.API.RateLimitOptions;
using SeekCasinoIO.Application;
using SeekCasinoIO.Infrastructure;
using SeekCasinoIO.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Rate Limit için HttpContext accessor
builder.Services.AddHttpContextAccessor();

// Rate Limit konfigürasyonu
builder.Services.ConfigureRateLimiting(builder.Configuration);

// Custom Rate Limit Configuration
builder.Services.AddSingleton<IRateLimitConfiguration, CustomRateLimitConfiguration>();

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SeekCasinoIO API", Version = "v1" });

    // JWT yetkilendirme için Swagger yapılandırması
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Add application layer services
builder.Services.AddApplicationServices();

// Add infrastructure layer services
builder.Services.AddInfrastructureServices(builder.Configuration);

// Add HTTP Logging
builder.Services.AddHttpLogging(logging =>
{
    logging.LoggingFields = HttpLoggingFields.All;
    logging.RequestBodyLogLimit = 4096;
    logging.ResponseBodyLogLimit = 4096;
});

// Add caching
builder.Services.AddDistributedMemoryCache();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Initialize database with default roles and admin user
await DatabaseInitializer.InitializeDatabaseAsync(app.Services);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Use CORS
app.UseCors("AllowAll");

// Use HTTP Logging
app.UseHttpLogging();

// RateLimit middleware'lerini kullan
app.UseRateLimitingMiddleware();

// Özel RateLimit middleware
app.UseMiddleware<RateLimitMiddleware>();

// Add global exception handling middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Authentication & Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();