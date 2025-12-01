namespace Casino.Domain.Entities;

public class Bet
{
    public Guid Id { get; set; }
    public Guid WalletId { get; set; }
    public string TelegramId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal WinAmount { get; set; }
    public string GameType { get; set; } = string.Empty;
    public string? TransactionHash { get; set; }
    public string? BetData { get; set; } // JSON string for game-specific data
    public string? GameResult { get; set; } // JSON string for game result
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public Wallet Wallet { get; set; } = null!;
}

