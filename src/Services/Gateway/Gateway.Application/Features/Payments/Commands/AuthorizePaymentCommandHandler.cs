using Gateway.Application.Interfaces;
using Gateway.Domain.Entities;
using Gateway.Domain.Events;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Gateway.Application.Features.Payments.Commands;

public class AuthorizePaymentCommandHandler : IRequestHandler<AuthorizePaymentCommand, AuthorizePaymentResult>
{
    private readonly ITransactionRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IDistributedCache _cache;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AuthorizePaymentCommandHandler> _logger;

    public AuthorizePaymentCommandHandler(
        ITransactionRepository repository,
        IPublishEndpoint publishEndpoint,
        IDistributedCache cache,
        IHttpClientFactory httpClientFactory,
        ILogger<AuthorizePaymentCommandHandler> logger)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
        _cache = cache;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<AuthorizePaymentResult> Handle(AuthorizePaymentCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        string idempotencyCacheKey = $"idempotency_{request.IdempotencyKey}";

        var cachedTransaction = await _cache.GetStringAsync(idempotencyCacheKey, cancellationToken);

        if (!string.IsNullOrEmpty(cachedTransaction))
        {
            _logger.LogInformation("Idempotency Cache HIT! DB'ye gidilmeden sonuç dönülüyor. Key: {Key}", request.IdempotencyKey);
            return JsonSerializer.Deserialize<AuthorizePaymentResult>(cachedTransaction)!;
        }

        var existingTransaction = await _repository.GetByIdempotencyKeyAsync(request.IdempotencyKey, cancellationToken);
        if (existingTransaction != null)
        {
            _logger.LogWarning("Cache MISS ama veritabanında bulundu! IdempotencyKey: {Key}", request.IdempotencyKey);

            var existingResult = new AuthorizePaymentResult { IsSuccess = true, Status = existingTransaction.Status, TransactionId = existingTransaction.Id };

            await _cache.SetStringAsync(idempotencyCacheKey, JsonSerializer.Serialize(existingResult),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24) }, cancellationToken);

            return existingResult;
        }

        string cacheKey = $"merchant_status_{request.MerchantId}";
        var merchantStatus = await _cache.GetStringAsync(cacheKey, cancellationToken);

        if (string.IsNullOrEmpty(merchantStatus))
        {
            try
            {
                var client = _httpClientFactory.CreateClient("MerchantClient");
                var response = await client.GetAsync($"/api/v1/merchants/{request.MerchantId}", cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    var merchantData = JsonSerializer.Deserialize<JsonElement>(content);
                    merchantStatus = merchantData.GetProperty("status").GetString();

                    await _cache.SetStringAsync(cacheKey, merchantStatus, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) }, cancellationToken);
                }
                else
                {
                    return new AuthorizePaymentResult { IsSuccess = false, ErrorMessage = "Geçersiz veya Pasif Üye İşyeri!" };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Merchant API'ye ulaşılamadı!");
                return new AuthorizePaymentResult { IsSuccess = false, ErrorMessage = "Üye işyeri doğrulama sistemi şu anda yanıt vermiyor." };
            }
        }

        if (merchantStatus != "Active")
            return new AuthorizePaymentResult { IsSuccess = false, ErrorMessage = "Üye işyeri aktif değil!" };

        var transaction = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            IdempotencyKey = request.IdempotencyKey,
            MerchantId = request.MerchantId,
            Amount = request.Amount,
            Currency = request.Currency,
            CardToken = request.CardToken,
            CreatedAt = DateTime.UtcNow
        };

        bool isApproved = request.CardToken == "TOKEN_SUCCESS"; 

        if (isApproved)
        {
            transaction.Status = "Approved";
            await _repository.AddAsync(transaction, cancellationToken);

            var paymentEvent = new PaymentApprovedEvent
            {
                TransactionId = transaction.Id,
                MerchantId = transaction.MerchantId,
                Amount = transaction.Amount,
                ApprovedAt = transaction.CreatedAt
            };

            await _publishEndpoint.Publish(paymentEvent, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            var successResult = new AuthorizePaymentResult { IsSuccess = true, Status = "Approved", TransactionId = transaction.Id };

            await _cache.SetStringAsync(idempotencyCacheKey, JsonSerializer.Serialize(successResult),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24) }, cancellationToken);

            return successResult;
        }

        transaction.Status = "Declined";
        await _repository.AddAsync(transaction, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return new AuthorizePaymentResult { IsSuccess = false, Status = "Declined", ErrorMessage = "Yetersiz Bakiye", TransactionId = transaction.Id };
    }
}