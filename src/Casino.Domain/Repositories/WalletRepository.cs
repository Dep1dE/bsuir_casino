using Casino.Domain.Data;
using Casino.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Casino.Domain.Repositories;

public class WalletRepository : IWalletRepository
{
    private readonly CasinoDbContext _context;

    public WalletRepository(CasinoDbContext context)
    {
        _context = context;
    }

    public async Task<Wallet?> GetByTelegramIdAsync(string telegramId, CancellationToken cancellationToken = default)
    {
        return await _context.Wallets
            .FirstOrDefaultAsync(w => w.TelegramId == telegramId, cancellationToken);
    }

    public async Task<Wallet?> GetByWalletAddressAsync(string walletAddress, CancellationToken cancellationToken = default)
    {
        return await _context.Wallets
            .FirstOrDefaultAsync(w => w.WalletAddress == walletAddress, cancellationToken);
    }

    public async Task<Wallet> CreateAsync(Wallet wallet, CancellationToken cancellationToken = default)
    {
        wallet.CreatedAt = DateTime.UtcNow;
        wallet.UpdatedAt = DateTime.UtcNow;
        _context.Wallets.Add(wallet);
        await _context.SaveChangesAsync(cancellationToken);
        return wallet;
    }

    public async Task<Wallet> UpdateAsync(Wallet wallet, CancellationToken cancellationToken = default)
    {
        wallet.UpdatedAt = DateTime.UtcNow;
        _context.Wallets.Update(wallet);
        await _context.SaveChangesAsync(cancellationToken);
        return wallet;
    }

    public async Task<bool> ExistsByTelegramIdAsync(string telegramId, CancellationToken cancellationToken = default)
    {
        return await _context.Wallets
            .AnyAsync(w => w.TelegramId == telegramId, cancellationToken);
    }
}

