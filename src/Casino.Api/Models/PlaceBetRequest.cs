using System.Text.Json.Serialization;

namespace Casino.Api.Models;

public class PlaceBetRequest
{
    [JsonPropertyName("telegram_id")]
    public string TelegramId { get; set; } = string.Empty;
    
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }
    
    [JsonPropertyName("game_type")]
    public string GameType { get; set; } = "slot";
    
    [JsonPropertyName("bet_data")]
    public Dictionary<string, object>? BetData { get; set; }
}

