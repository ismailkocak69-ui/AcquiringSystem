using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/api/v1/merchants/{id}", (string id) =>
{
    bool isActive = id == "MERCHANT_XYZ";

    if (isActive)
    {
        return Results.Ok(new
        {
            MerchantId = id,
            Status = "Active",
            DailyLimit = 50000
        });
    }

    return Results.NotFound(new { Error = "Üye işyeri bulunamadı veya pasif." });
});

//app.Run("http://localhost:5002");
app.Run();