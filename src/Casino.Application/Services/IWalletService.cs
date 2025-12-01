using System.Text.Json.Serialization;

namespace Casino.Application.Services;

public interface IWalletService
{
    Task<CreateWalletResponse> CreateWalletAsync(string telegramId, CancellationToken cancellationToken = default);
    Task<GetBalanceResponse> GetBalanceAsync(string telegramId, CancellationToken cancellationToken = default);
    Task<DepositResponse> DepositAsync(string telegramId, decimal amount, CancellationToken cancellationToken = default);
}

public record DepositResponse(
    bool Success,
    string? TransactionHash,
    decimal NewBalance,
    string? Message
);

public record CreateWalletResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("wallet_address")] string? WalletAddress,
    [property: JsonPropertyName("balance")] decimal Balance,
    [property: JsonPropertyName("message")] string? Message
);

public record GetBalanceResponse(
    [property: JsonPropertyName("balance")] decimal Balance,
    [property: JsonPropertyName("wallet_address")] string? WalletAddress
);

