using Elastic.Ingest.Elasticsearch.DataStreams;
using Elastic.Serilog.Sinks;
using Gateway.Api.Consumers;
using Gateway.Infrastructure.Data;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.OpenApi;
using Scalar.AspNetCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .WriteTo.Console()
    .WriteTo.Elasticsearch(new[] { new Uri("http://elasticsearch:9200") }, options =>
    {
        options.DataStream = new DataStreamName("logs", "gateway", "dev");
    })
    .Enrich.WithProperty("Environment", "Development")
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ==========================================
// YENİ NESİL .NET 10 OPENAPI (v4) VE SCALAR AYARI
// ==========================================
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Components ??= new OpenApiComponents();

        // ÇÖZÜM 1: Dictionary artık IOpenApiSecurityScheme (Interface) tipinde olmalı
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Keycloak'tan aldığınız token'ı buraya yapıştırın."
        };

        document.Security ??= new List<OpenApiSecurityRequirement>();

        // ÇÖZÜM 2 & 3: Id özelliğini atamak yerine constructor'a "Bearer" ve document'i gönderiyoruz!
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
        });

        return Task.CompletedTask;
    });
});

// ==========================================
// VERİTABANI, MASSTRANSIT VE HTTPCLIENT
// ==========================================
builder.Services.AddDbContext<GatewayDbContext>(options =>
{
    options.UseNpgsql("Host=postgres;Database=AcquiringDb;Username=postgres;Password=AcquiringSecretPass1!");
});

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<PaymentApprovedEventConsumer>();
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("rabbitmq", "/", h => {
            h.Username("guest");
            h.Password("guest");
        });
        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddHttpClient("MerchantClient", client =>
{
    client.BaseAddress = new Uri("http://merchant_api_container:5002");
})
.AddStandardResilienceHandler(options =>
{
    options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(2);
    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(10);
    options.CircuitBreaker.FailureRatio = 0.5;
    options.Retry.MaxRetryAttempts = 3;
});

// ==========================================
// KEYCLOAK GÜVENLİK AYARLARI
// ==========================================
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MetadataAddress = "http://keycloak:8080/realms/AcquiringRealm/.well-known/openid-configuration";
        options.RequireHttpsMetadata = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            // İmzayı ve süreyi KESİNLİKLE doğrula (Güvenliğin kalbi burasıdır)
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,

            // Local ortamda Audience ve Issuer katı kontrollerini esnetiyoruz
            ValidateAudience = false,
            ValidateIssuer = false
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddDistributedMemoryCache();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<GatewayDbContext>();
    dbContext.Database.Migrate();
}

// ==========================================
// PIPELINE (UI VE ROTALAR)
// ==========================================
if (app.Environment.IsDevelopment())
{
    // Eski app.UseSwagger() yerine modern metotlar:
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();