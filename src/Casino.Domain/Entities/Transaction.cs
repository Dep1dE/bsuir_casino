namespace Casino.Domain.Entities;

public class Transaction
{
    public Guid Id { get; set; }
    public Guid WalletId { get; set; }
    public string TransactionHash { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string TransactionType { get; set; } = string.Empty; // "bet", "win", "deposit", "withdraw"
    public string Status { get; set; } = string.Empty; // "pending", "confirmed", "failed"
    public DateTime CreatedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }

    // Navigation properties
    public Wallet Wallet { get; set; } = null!;
}

