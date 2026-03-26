namespace Gateway.Domain.Events;

public class SettlementFailedEvent
{
    public Guid TransactionId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime FailedAt { get; set; }
}