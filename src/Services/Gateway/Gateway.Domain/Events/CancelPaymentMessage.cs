namespace Gateway.Domain.Events;

public class CancelPaymentMessage
{
    public Guid TransactionId { get; set; }
    public string Reason { get; set; } = string.Empty;
}