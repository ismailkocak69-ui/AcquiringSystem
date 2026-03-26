using Gateway.Application.Interfaces;
using Gateway.Domain.Events;
using MassTransit;

namespace Gateway.Api.Consumers;

public class CancelPaymentConsumer : IConsumer<CancelPaymentMessage>
{
    private readonly ITransactionRepository _repository;
    private readonly ILogger<CancelPaymentConsumer> _logger;

    public CancelPaymentConsumer(ITransactionRepository repository, ILogger<CancelPaymentConsumer> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CancelPaymentMessage> context)
    {
        var message = context.Message;
        _logger.LogWarning(">>> TELAFİ İŞLEMİ (COMPENSATION) BAŞLADI! TransactionId: {Id}, Sebep: {Reason} <<<",
            message.TransactionId, message.Reason);

        // 1. Veritabanından orijinal işlemi bul
        var transaction = await _repository.GetByIdAsync(message.TransactionId);

        if (transaction != null)
        {
            // 2. Durumunu güncelle (Refunded / Failed)
            transaction.Status = "Refunded";

            // 3. Veritabanına kaydet
            await _repository.SaveChangesAsync();

            _logger.LogInformation(">>> İŞLEM BAŞARIYLA İPTAL EDİLDİ (REFUNDED). Veri tutarlılığı sağlandı. <<<");
        }
        else
        {
            _logger.LogError("İptal edilecek Transaction bulunamadı! Id: {Id}", message.TransactionId);
        }
    }
}