using FluentValidation;
using Gateway.Application.DTOs;
using Gateway.Application.Features.Payments.Commands;
using Gateway.Application.Features.Payments.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IValidator<PaymentRequest> _validator;

    public PaymentsController(IMediator mediator, IValidator<PaymentRequest> validator)
    {
        _mediator = mediator;
        _validator = validator;
    }

    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions()
    {
        var transactions = await _mediator.Send(new GetTransactionsQuery());
        return Ok(transactions);
    }

    [HttpPost("authorize")]
    public async Task<IActionResult> AuthorizePayment([FromBody] PaymentRequest request)
    {
        var validationResult = await _validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors.Select(e => new { Field = e.PropertyName, Error = e.ErrorMessage }));
        }

        var result = await _mediator.Send(new AuthorizePaymentCommand(request));

        if (!result.IsSuccess)
        {
            return BadRequest(new { Error = result.ErrorMessage, Status = result.Status, TransactionId = result.TransactionId });
        }

        return Ok(new { Status = result.Status, TransactionId = result.TransactionId });
    }
}