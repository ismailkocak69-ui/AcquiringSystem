using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Gateway.Api.Controllers;
using Gateway.Application.DTOs;
using Gateway.Application.Features.Payments.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Gateway.UnitTests;

public class PaymentsControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<IValidator<PaymentRequest>> _validatorMock;
    private readonly PaymentsController _controller;

    public PaymentsControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _validatorMock = new Mock<IValidator<PaymentRequest>>();

        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<PaymentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _controller = new PaymentsController(_mediatorMock.Object, _validatorMock.Object);
    }

    [Fact]
    public async Task AuthorizePayment_WhenValidationFails_ShouldReturnBadRequest()
    {
        var request = new PaymentRequest { Amount = 0 };
        var validationFailures = new List<ValidationFailure> { new ValidationFailure("Amount", "Tutar 0'dan büyük olmalı") };

        _validatorMock
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        var result = await _controller.AuthorizePayment(request);

        result.Should().BeOfType<BadRequestObjectResult>();
        _mediatorMock.Verify(m => m.Send(It.IsAny<AuthorizePaymentCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AuthorizePayment_WhenCommandFails_ShouldReturnBadRequest()
    {
        var request = new PaymentRequest { Amount = 100 };
        var commandResult = new AuthorizePaymentResult { IsSuccess = false, ErrorMessage = "Yetersiz Bakiye" };

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<AuthorizePaymentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(commandResult);

        var result = await _controller.AuthorizePayment(request);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task AuthorizePayment_WhenCommandSucceeds_ShouldReturnOk()
    {
        var request = new PaymentRequest { Amount = 100 };
        var commandResult = new AuthorizePaymentResult { IsSuccess = true, Status = "Approved", TransactionId = Guid.NewGuid() };

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<AuthorizePaymentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(commandResult);

        var result = await _controller.AuthorizePayment(request);

        result.Should().BeOfType<OkObjectResult>();
    }
}