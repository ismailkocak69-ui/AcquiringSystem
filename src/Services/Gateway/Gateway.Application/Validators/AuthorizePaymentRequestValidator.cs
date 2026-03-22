using FluentValidation;
using Gateway.Application.DTOs;

namespace Gateway.Application.Validators;

public class PaymentRequestValidator : AbstractValidator<PaymentRequest>
{
    public PaymentRequestValidator()
    {
        RuleFor(x => x.IdempotencyKey)
            .NotEmpty().WithMessage("IdempotencyKey boş olamaz. Tekrarlı işlemleri önlemek için zorunludur.");

        RuleFor(x => x.MerchantId)
            .NotEmpty().WithMessage("MerchantId zorunludur.");

        RuleFor(x => x.CardToken)
            .NotEmpty().WithMessage("CardToken boş olamaz.")
            .MinimumLength(10).WithMessage("Geçersiz CardToken formatı.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Ödeme tutarı 0'dan büyük olmalıdır.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Para birimi (Currency) boş olamaz.")
            .Must(BeAValidCurrency).WithMessage("Sadece 'TRY', 'USD' veya 'EUR' para birimleri desteklenmektedir.");
    }

    private bool BeAValidCurrency(string currency)
    {
        return currency == "TRY" || currency == "USD" || currency == "EUR";
    }
}