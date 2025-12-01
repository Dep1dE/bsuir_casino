using Casino.Domain.Data;
using Casino.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Casino.Domain.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly CasinoDbContext _context;

    public TransactionRepository(CasinoDbContext context)
    {
        _context = context;
    }

    public async Task<Transaction> CreateAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        transaction.CreatedAt = DateTime.UtcNow;
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync(cancellationToken);
        return transaction;
    }

    public async Task<Transaction?> GetByHashAsync(string transactionHash, CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .FirstOrDefaultAsync(t => t.TransactionHash == transactionHash, cancellationToken);
    }

    public async Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        _context.Transactions.Update(transaction);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

