using Casino.Domain.Entities;

namespace Casino.Domain.Repositories;

public interface IWalletRepository
{
    Task<Wallet?> GetByTelegramIdAsync(string telegramId, CancellationToken cancellationToken = default);
    Task<Wallet?> GetByWalletAddressAsync(string walletAddress, CancellationToken cancellationToken = default);
    Task<Wallet> CreateAsync(Wallet wallet, CancellationToken cancellationToken = default);
    Task<Wallet> UpdateAsync(Wallet wallet, CancellationToken cancellationToken = default);
    Task<bool> ExistsByTelegramIdAsync(string telegramId, CancellationToken cancellationToken = default);
}

