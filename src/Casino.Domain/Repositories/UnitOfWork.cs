using Casino.Domain.Data;

namespace Casino.Domain.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly CasinoDbContext _context;
    private IWalletRepository? _wallets;
    private IBetRepository? _bets;
    private ITransactionRepository? _transactions;

    public UnitOfWork(CasinoDbContext context)
    {
        _context = context;
    }

    public IWalletRepository Wallets =>
        _wallets ??= new WalletRepository(_context);

    public IBetRepository Bets =>
        _bets ??= new BetRepository(_context);

    public ITransactionRepository Transactions =>
        _transactions ??= new TransactionRepository(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}

