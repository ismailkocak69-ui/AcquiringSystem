namespace Gateway.Domain.Entities
{
    public class PaymentTransaction
    {
        public Guid Id { get; set; }
        public Guid IdempotencyKey { get; set; }
        public string MerchantId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string CardToken { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}