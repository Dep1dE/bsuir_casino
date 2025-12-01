namespace Casino.Application.Services;

public interface ISolanaService
{
    Task<(byte[] PublicKey, string PrivateKey)> CreateWalletAsync(CancellationToken cancellationToken = default);
    Task<decimal> GetBalanceAsync(string walletAddress, CancellationToken cancellationToken = default);
    Task<string> SendTransactionAsync(string fromPrivateKey, string toPublicKey, decimal amount, CancellationToken cancellationToken = default);
    Task<string?> RequestAirdropAsync(string walletAddress, decimal amount, CancellationToken cancellationToken = default);
}

