using Gateway.Api.Events;
using MassTransit;

namespace Gateway.Api.Consumers
{
    public class PaymentApprovedEventConsumer : IConsumer<PaymentApprovedEvent>
    {
        private readonly ILogger<PaymentApprovedEventConsumer> _logger;

        public PaymentApprovedEventConsumer(ILogger<PaymentApprovedEventConsumer> logger)
        {
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<PaymentApprovedEvent> context)
        {
            var paymentEvent = context.Message;

            _logger.LogWarning(">>> [SETTLEMENT SÜRECİ BAŞLADI] <<<");
            _logger.LogInformation("Gelen İşlem ID: {TransactionId}, Üye İşyeri: {MerchantId}",
                paymentEvent.TransactionId, paymentEvent.MerchantId);

            await Task.Delay(2000);

            _logger.LogWarning(">>> [SETTLEMENT SÜRECİ BİTTİ] {Amount} TRY tutarındaki hak ediş hesaplandı ve kaydedildi. <<<",
                paymentEvent.Amount);
        }
    }
}