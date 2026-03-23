using MediatR;
using Gateway.Application.DTOs;

namespace Gateway.Application.Features.Payments.Commands;

public class AuthorizePaymentResult
{
    public bool IsSuccess { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? TransactionId { get; set; }
    public string? ErrorMessage { get; set; }
}

public class AuthorizePaymentCommand : IRequest<AuthorizePaymentResult>
{
    public PaymentRequest Request { get; set; }

    public AuthorizePaymentCommand(PaymentRequest request)
    {
        Request = request;
    }
}