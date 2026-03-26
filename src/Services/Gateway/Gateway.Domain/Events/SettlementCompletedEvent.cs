namespace Gateway.Domain.Events;

public class SettlementCompletedEvent
{
    public Guid TransactionId { get; set; }
    public DateTime CompletedAt { get; set; }
}