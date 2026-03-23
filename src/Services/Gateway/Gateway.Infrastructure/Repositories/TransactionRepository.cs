using Gateway.Application.Interfaces;
using Gateway.Domain.Entities;
using Gateway.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Gateway.Infrastructure.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly GatewayDbContext _dbContext;

    public TransactionRepository(GatewayDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<PaymentTransaction>> GetAllTransactionsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Transactions
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }
    public async Task<PaymentTransaction?> GetByIdempotencyKeyAsync(Guid idempotencyKey, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Transactions.FirstOrDefaultAsync(t => t.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    public async Task AddAsync(PaymentTransaction transaction, CancellationToken cancellationToken = default)
    {
        await _dbContext.Transactions.AddAsync(transaction, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}