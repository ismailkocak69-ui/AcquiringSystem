using FluentAssertions;
using FluentValidation;
using Gateway.Api.Controllers;
using Gateway.Application.DTOs;
using Gateway.Infrastructure.Data;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gateway.UnitTests
{
    public class PaymentsControllerTests
    {
        private readonly Mock<ILogger<PaymentsController>> _mockLogger;
        private readonly Mock<IPublishEndpoint> _mockPublishEndpoint;
        private readonly Mock<IDistributedCache> _mockCache;
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly GatewayDbContext _dbContext;
        private readonly Mock<IValidator<PaymentRequest>> _mockValidator;

        public PaymentsControllerTests()
        {
            _mockLogger = new Mock<ILogger<PaymentsController>>();
            _mockPublishEndpoint = new Mock<IPublishEndpoint>();
            _mockCache = new Mock<IDistributedCache>();
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            _mockValidator = new Mock<IValidator<PaymentRequest>>();

            _mockValidator
                .Setup(v => v.ValidateAsync(It.IsAny<PaymentRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            var dbOptions = new DbContextOptionsBuilder<GatewayDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new GatewayDbContext(dbOptions);
        }

        [Fact]
        public async Task AuthorizePayment_WhenAmountIsZeroOrLess_ShouldReturnBadRequest()
        {
            // 1. ARRANGE
            _mockCache.Setup(x => x.GetAsync(It.IsAny<string>(), default))
                      .ReturnsAsync(System.Text.Encoding.UTF8.GetBytes("Active"));

            var controller = new PaymentsController(
                _mockLogger.Object,
                _mockPublishEndpoint.Object,
                _dbContext,
                _mockCache.Object,
                _mockHttpClientFactory.Object,
                _mockValidator.Object
            );

            var request = new PaymentRequest
            {
                IdempotencyKey = Guid.NewGuid(),
                MerchantId = "MERCHANT_XYZ",
                CardToken = "TOKEN_SUCCESS",
                Currency = "TRY",
                Amount = 0
            };

            // 2. ACT
            var result = await controller.AuthorizePayment(request);

            // 3. ASSERT
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;

            var errorResponse = badRequestResult.Value;
            errorResponse.Should().NotBeNull();

            var errorProperty = errorResponse.GetType().GetProperty("Error").GetValue(errorResponse, null);
            errorProperty.Should().Be("Tutar 0'dan büyük olmalıdır.");
        }
    }
}