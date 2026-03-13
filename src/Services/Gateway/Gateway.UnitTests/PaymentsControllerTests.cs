using System;
using System.Threading.Tasks;
using FluentAssertions;
using Gateway.Api.Controllers;
using Gateway.Api.Models;
using Gateway.Infrastructure.Data;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net.Http;
using Xunit;

namespace Gateway.UnitTests
{
    public class PaymentsControllerTests
    {
        // Bağımlılıkları (Dependencies) taklit etmek için Moq kullanıyoruz
        private readonly Mock<ILogger<PaymentsController>> _mockLogger;
        private readonly Mock<IPublishEndpoint> _mockPublishEndpoint;
        private readonly Mock<IDistributedCache> _mockCache;
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly GatewayDbContext _dbContext;

        public PaymentsControllerTests()
        {
            _mockLogger = new Mock<ILogger<PaymentsController>>();
            _mockPublishEndpoint = new Mock<IPublishEndpoint>();
            _mockCache = new Mock<IDistributedCache>();
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();

            // Veritabanını taklit etmek için In-Memory (RAM) DB kullanıyoruz
            var dbOptions = new DbContextOptionsBuilder<GatewayDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Her test için yepyeni, temiz bir DB
                .Options;

            _dbContext = new GatewayDbContext(dbOptions);
        }

        [Fact] // XUnit'e bunun bir test metodu olduğunu söylüyoruz
        public async Task AuthorizePayment_WhenAmountIsZeroOrLess_ShouldReturnBadRequest()
        {
            // 1. ARRANGE (HAZIRLIK EVRESİ)
            // Sahte Cache'imizin Merchant'ı "Active" olarak dönmesini sağlıyoruz ki hata Cache'den dönmesin
            _mockCache.Setup(x => x.GetAsync(It.IsAny<string>(), default))
                      .ReturnsAsync(System.Text.Encoding.UTF8.GetBytes("Active"));

            var controller = new PaymentsController(
                _mockLogger.Object,
                _mockPublishEndpoint.Object,
                _dbContext,
                _mockCache.Object,
                _mockHttpClientFactory.Object
            );

            var request = new PaymentRequest
            {
                IdempotencyKey = Guid.NewGuid(),
                MerchantId = "MERCHANT_XYZ",
                CardToken = "TOKEN_SUCCESS",
                Currency = "TRY",
                Amount = 0 // BİLEREK HATALI TUTAR VERİYORUZ!
            };

            // 2. ACT (EYLEM EVRESİ)
            var result = await controller.AuthorizePayment(request);

            // 3. ASSERT (DOĞRULAMA EVRESİ - TDD'nin kalbi)
            // Sonucun bir BadRequestObjectResult (HTTP 400) olmasını bekliyoruz
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;

            // Dönen hatanın içindeki Error mesajının bizim beklediğimiz mesaj olmasını doğruluyoruz
            var errorResponse = badRequestResult.Value;
            errorResponse.Should().NotBeNull();

            // FluentAssertions kullanarak JSON objesinin içindeki "Error" property'sini okuyoruz
            var errorProperty = errorResponse.GetType().GetProperty("Error").GetValue(errorResponse, null);
            errorProperty.Should().Be("Tutar 0'dan büyük olmalıdır.");
        }
    }
}