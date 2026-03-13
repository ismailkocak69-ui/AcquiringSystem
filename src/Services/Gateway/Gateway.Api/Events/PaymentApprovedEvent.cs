namespace Gateway.Api.Events
{
    public record PaymentApprovedEvent
    {
        public Guid TransactionId { get; init; }
        public string MerchantId { get; init; }
        public decimal Amount { get; init; }
        public DateTime ApprovedAt { get; init; }
    }
}