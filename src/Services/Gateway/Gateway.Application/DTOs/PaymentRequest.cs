namespace Gateway.Application.DTOs
{
    public class PaymentRequest
    {
        public Guid IdempotencyKey { get; set; }
        public string CardToken { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string MerchantId { get; set; }
    }
}