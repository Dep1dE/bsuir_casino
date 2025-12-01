using System.Text.Json.Serialization;

namespace Casino.Api.Models;

public class CreateWalletRequest
{
    [JsonPropertyName("telegram_id")]
    public string TelegramId { get; set; } = string.Empty;
}

