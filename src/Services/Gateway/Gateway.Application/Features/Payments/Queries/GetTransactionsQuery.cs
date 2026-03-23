using Gateway.Application.Interfaces;
using Gateway.Domain.Entities;
using MediatR;

namespace Gateway.Application.Features.Payments.Queries;

public class GetTransactionsQuery : IRequest<List<PaymentTransaction>>
{
}

public class GetTransactionsQueryHandler : IRequestHandler<GetTransactionsQuery, List<PaymentTransaction>>
{
    private readonly ITransactionRepository _repository;

    public GetTransactionsQueryHandler(ITransactionRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<PaymentTransaction>> Handle(GetTransactionsQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetAllTransactionsAsync(cancellationToken);
    }
}