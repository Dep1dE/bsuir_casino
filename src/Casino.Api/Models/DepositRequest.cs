namespace Casino.Api.Models;

public class DepositRequest
{
    public string TelegramId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

