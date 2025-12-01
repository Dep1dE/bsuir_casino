using System.Text.Json.Serialization;

namespace Casino.Application.Services;

public interface IBetService
{
    Task<PlaceBetResponse> PlaceBetAsync(PlaceBetRequest request, CancellationToken cancellationToken = default);
}

public record PlaceBetRequest(
    string TelegramId,
    decimal Amount,
    string GameType,
    Dictionary<string, object>? BetData
);

public record PlaceBetResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("transaction_hash")] string? TransactionHash,
    [property: JsonPropertyName("win_amount")] decimal WinAmount,
    [property: JsonPropertyName("balance")] decimal Balance,
    [property: JsonPropertyName("message")] string? Message,
    [property: JsonPropertyName("winning_combination")] List<string>? WinningCombination
);

