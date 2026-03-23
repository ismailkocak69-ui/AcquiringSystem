using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using Merchant.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString = "Host=postgres_container;Database=MerchantDb;Username=postgres;Password=AcquiringSecretPass1!";
builder.Services.AddDbContext<MerchantDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddDistributedMemoryCache();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MerchantDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation(">>> Merchant veritabanı Migrations uygulanıyor... <<<");

        db.Database.Migrate();

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

app.MapGet("/api/v1/merchants/{id}", async (string id, MerchantDbContext db, IDistributedCache cache) =>
{
    var cacheKey = $"merchant_info_{id}";

    var cachedData = await cache.GetStringAsync(cacheKey);
    if (!string.IsNullOrEmpty(cachedData))
    {
        var cachedMerchant = JsonSerializer.Deserialize<MerchantEntity>(cachedData);
        return Results.Ok(cachedMerchant);
    }

    var merchantFromDb = await db.Merchants.FindAsync(id);
    if (merchantFromDb == null)
    {
        return Results.NotFound(new { Error = "Üye işyeri bulunamadı veya pasif." });
    }

    var serializedData = JsonSerializer.Serialize(merchantFromDb);
    await cache.SetStringAsync(cacheKey, serializedData, new DistributedCacheEntryOptions
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
    });

    return Results.Ok(merchantFromDb);
});

app.Run();