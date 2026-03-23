using Gateway.Domain.Entities;

namespace Gateway.Application.Interfaces;

public interface ITransactionRepository
{
    Task<List<PaymentTransaction>> GetAllTransactionsAsync(CancellationToken cancellationToken = default);
    Task<PaymentTransaction?> GetByIdempotencyKeyAsync(Guid idempotencyKey, CancellationToken cancellationToken = default);
    Task AddAsync(PaymentTransaction transaction, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}