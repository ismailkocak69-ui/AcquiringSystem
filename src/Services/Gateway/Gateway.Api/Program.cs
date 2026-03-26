using Elastic.Ingest.Elasticsearch.DataStreams;
using Elastic.Serilog.Sinks;
using FluentValidation;
using Gateway.Api.Consumers;
using Gateway.Application.Interfaces;
using Gateway.Application.Sagas;
using Gateway.Application.Validators;
using Gateway.Infrastructure.Data;
using Gateway.Infrastructure.Repositories;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Serilog;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

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

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("Gateway.Api"))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSource("Npgsql")
            .AddSource("MassTransit")
            .AddOtlpExporter(opts =>
            {
                opts.Endpoint = new Uri("http://jaeger:4317");
            });
    });

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Components ??= new OpenApiComponents();

        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Keycloak'tan aldığınız token'ı buraya yapıştırın."
        };

        document.Security ??= new List<OpenApiSecurityRequirement>();

        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
        });

        return Task.CompletedTask;
    });
});

builder.Services.AddDbContext<GatewayDbContext>(options =>
{
    options.UseNpgsql("Host=postgres;Database=AcquiringDb;Username=postgres;Password=AcquiringSecretPass1!");
});

builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    x.AddSagaStateMachine<PaymentStateMachine, PaymentState>()
        .EntityFrameworkRepository(r =>
        {
            r.ConcurrencyMode = ConcurrencyMode.Pessimistic;
            r.ExistingDbContext<GatewayDbContext>();
            r.UsePostgres();
        });

    x.AddEntityFrameworkOutbox<GatewayDbContext>(o =>
    {
        o.UsePostgres();
        o.UseBusOutbox();
    });

    x.AddConsumer<PaymentApprovedEventConsumer>();
    x.AddConsumer<CancelPaymentConsumer>();

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

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MetadataAddress = "http://keycloak:8080/realms/AcquiringRealm/.well-known/openid-configuration";
        options.RequireHttpsMetadata = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,

            ValidateAudience = false,
            ValidateIssuer = false
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddDistributedMemoryCache();

builder.Services.AddValidatorsFromAssemblyContaining<PaymentRequestValidator>();

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<Gateway.Application.DTOs.PaymentRequest>();
});

builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<GatewayDbContext>();
    dbContext.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();