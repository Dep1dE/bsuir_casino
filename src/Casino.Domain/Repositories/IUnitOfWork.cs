namespace Casino.Domain.Repositories;

public interface IUnitOfWork : IDisposable
{
    IWalletRepository Wallets { get; }
    IBetRepository Bets { get; }
    ITransactionRepository Transactions { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

