using FluentValidation;
using Gateway.Api.Events;
using Gateway.Application.DTOs;
using Gateway.Domain.Entities;
using Gateway.Infrastructure.Data;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Net.Http;
using System.Text.Json;

namespace Gateway.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/v1/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly ILogger<PaymentsController> _logger;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly GatewayDbContext _dbContext;
        private readonly IDistributedCache _cache;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IValidator<PaymentRequest> _validator;

        public PaymentsController(
            ILogger<PaymentsController> logger,
            IPublishEndpoint publishEndpoint,
            GatewayDbContext dbContext,
            IDistributedCache cache,
            IHttpClientFactory httpClientFactory, 
            IValidator<PaymentRequest> validator)
        {
            _logger = logger;
            _publishEndpoint = publishEndpoint;
            _dbContext = dbContext;
            _cache = cache;
            _httpClientFactory = httpClientFactory;
            _validator = validator;
        }

        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactions()
        {
            var transactions = await _dbContext.Transactions.OrderByDescending(x => x.CreatedAt).ToListAsync();
            return Ok(transactions);
        }

        [HttpPost("authorize")]
        public async Task<IActionResult> AuthorizePayment([FromBody] PaymentRequest request)
        {
            var validationResult = await _validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Geçersiz ödeme isteği alındı. Hatalar: {@Errors}", validationResult.Errors);
                return BadRequest(validationResult.Errors.Select(e => new { Field = e.PropertyName, Error = e.ErrorMessage }));
            }

            _logger.LogInformation("Ödeme isteği alındı. IdempotencyKey: {IdempotencyKey}", request.IdempotencyKey);

            var existingTransaction = await _dbContext.Transactions
                .FirstOrDefaultAsync(t => t.IdempotencyKey == request.IdempotencyKey);

            if (existingTransaction != null)
            {
                _logger.LogWarning(">>> MÜKERRER İŞLEM YAKALANDI! IdempotencyKey: {Key} <<<", request.IdempotencyKey);

                return Ok(new
                {
                    Status = existingTransaction.Status,
                    TransactionId = existingTransaction.Id,
                    Message = "Duplicate request. Returning original response."
                });
            }

            string cacheKey = $"merchant_status_{request.MerchantId}";
            var merchantStatus = await _cache.GetStringAsync(cacheKey);

            if (string.IsNullOrEmpty(merchantStatus))
            {
                _logger.LogWarning(">>> Merchant verisi Cache'de YOK! Merchant API'ye soruluyor... <<<");

                try
                {
                    var client = _httpClientFactory.CreateClient("MerchantClient");
                    var response = await client.GetAsync($"/api/v1/merchants/{request.MerchantId}");

                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var merchantData = JsonSerializer.Deserialize<JsonElement>(content);

                        merchantStatus = merchantData.GetProperty("status").GetString();

                        await _cache.SetStringAsync(cacheKey, merchantStatus, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) });
                        _logger.LogInformation(">>> Merchant API'den veri alındı ve CACHE'e yazıldı: {Status} <<<", merchantStatus);
                    }
                    else
                    {
                        _logger.LogError(">>> Merchant API hatası veya Merchant bulunamadı! <<<");
                        return BadRequest(new { Error = "Geçersiz veya Pasif Üye İşyeri!" });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ">>> ACİL DURUM: Merchant API'ye ulaşılamadı! Şalter inmiş veya servis çökmüş olabilir. <<<");

                    return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                    {
                        Error = "Üye işyeri doğrulama sistemi şu anda yanıt vermiyor. Lütfen kısa süre sonra tekrar deneyin."
                    });
                }
            }
            else
            {
                _logger.LogInformation(">>> Merchant verisi ultra hızlı CACHE'DEN okundu! <<<");
            }

            if (merchantStatus != "Active")
                return BadRequest(new { Error = "Üye işyeri aktif değil!" });

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

            bool isApproved = SimulateBankSwitch(request.CardToken);

            if (isApproved)
            {
                transaction.Status = "Approved";
                _dbContext.Transactions.Add(transaction);

                var paymentEvent = new PaymentApprovedEvent
                {
                    TransactionId = transaction.Id,
                    MerchantId = transaction.MerchantId,
                    Amount = transaction.Amount,
                    ApprovedAt = transaction.CreatedAt
                };

                _logger.LogInformation("Ödeme başarılı. RabbitMQ event'i Outbox'a alınıyor. TransactionId: {TransactionId}",
                    nameof(PaymentApprovedEvent),
                    paymentEvent.TransactionId);

                await _publishEndpoint.Publish(paymentEvent);
                await _dbContext.SaveChangesAsync();

                return Ok(new { Status = "Approved", TransactionId = transaction.Id });
            }

            transaction.Status = "Declined";
            _dbContext.Transactions.Add(transaction);
            await _dbContext.SaveChangesAsync();

            return BadRequest(new { Status = "Declined", Reason = "Yetersiz Bakiye", TransactionId = transaction.Id });
        }

        private bool SimulateBankSwitch(string cardToken)
        {
            return cardToken == "TOKEN_SUCCESS";
        }
    }
}