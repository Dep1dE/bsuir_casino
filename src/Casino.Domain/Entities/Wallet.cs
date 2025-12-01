namespace Casino.Domain.Entities;

public class Wallet
{
    public Guid Id { get; set; }
    public string TelegramId { get; set; } = string.Empty;
    public string WalletAddress { get; set; } = string.Empty;
    public string EncryptedPrivateKey { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<Bet> Bets { get; set; } = new List<Bet>();
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}

