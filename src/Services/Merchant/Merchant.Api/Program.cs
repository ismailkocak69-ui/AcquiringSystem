using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using Merchant.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// 1. Veritabanı (PostgreSQL) - DDD kuralı: Kendi veritabanı "MerchantDb"
var connectionString = "Host=postgres_container;Database=MerchantDb;Username=postgres;Password=AcquiringSecretPass1!";
builder.Services.AddDbContext<MerchantDbContext>(options =>
    options.UseNpgsql(connectionString));

// 2. Önbellek (Cache) - Redis ile aynı arayüzü kullanan In-Memory Distributed Cache
builder.Services.AddDistributedMemoryCache();

var app = builder.Build();

// 3. Otomatik Veritabanı Kurulumu (Migrations) ve Başlangıç (Seed) Verisi
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MerchantDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation(">>> Merchant veritabanı Migrations uygulanıyor... <<<");

        // EnsureCreated YERİNE Migrate() kullanıyoruz! Tabloyu kesin olarak oluşturur.
        db.Database.Migrate();

        // Testlerimiz patlamasın diye Gateway'in aradığı "MERCHANT_123"ü içeri atıyoruz
        if (!db.Merchants.Any(m => m.Id == "MERCHANT_123"))
        {
            db.Merchants.Add(new MerchantEntity
            {
                Id = "MERCHANT_123",
                Name = "Premium İşyeri",
                Status = "Active",
                DailyLimit = 50000
            });
            db.SaveChanges();
            logger.LogInformation(">>> Seed veri eklendi: MERCHANT_123 <<<");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, ">>> ACİL DURUM: Veritabanı tabloları oluşturulurken hata fırlattı! <<<");
    }
}

// 4. Efsanevi Cache-Aside Endpoint'i
app.MapGet("/api/v1/merchants/{id}", async (string id, MerchantDbContext db, IDistributedCache cache) =>
{
    var cacheKey = $"merchant_info_{id}";

    // AŞAMA 1: Önce Cache'e (Hafıza) Bak
    var cachedData = await cache.GetStringAsync(cacheKey);
    if (!string.IsNullOrEmpty(cachedData))
    {
        var cachedMerchant = JsonSerializer.Deserialize<MerchantEntity>(cachedData);
        // İstersen test için buraya Console.WriteLine("Cache'den geldi!") ekleyebilirsin
        return Results.Ok(cachedMerchant);
    }

    // AŞAMA 2: Cache'de yoksa Veritabanına (Postgres) İn
    var merchantFromDb = await db.Merchants.FindAsync(id);
    if (merchantFromDb == null)
    {
        return Results.NotFound(new { Error = "Üye işyeri bulunamadı veya pasif." });
    }

    // AŞAMA 3: Veritabanından geleni Cache'e yaz (Örn: 10 Dakika boyunca DB'ye inme)
    var serializedData = JsonSerializer.Serialize(merchantFromDb);
    await cache.SetStringAsync(cacheKey, serializedData, new DistributedCacheEntryOptions
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
    });

    // AŞAMA 4: Sonucu Dön
    return Results.Ok(merchantFromDb);
});

app.Run();