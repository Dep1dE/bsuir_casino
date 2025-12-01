using Casino.Domain.Entities;

namespace Casino.Domain.Repositories;

public interface ITransactionRepository
{
    Task<Transaction> CreateAsync(Transaction transaction, CancellationToken cancellationToken = default);
    Task<Transaction?> GetByHashAsync(string transactionHash, CancellationToken cancellationToken = default);
    Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default);
}

