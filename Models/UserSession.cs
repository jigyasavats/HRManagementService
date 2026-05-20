using Newtonsoft.Json;

namespace HRManagementService.Models;

public class UserSession
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("alias")]
    public string Alias { get; set; } = string.Empty;

    [JsonProperty("role")]
    public string Role { get; set; } = string.Empty;

    [JsonProperty("tokenId")]
    public string TokenId { get; set; } = string.Empty;

    [JsonProperty("loginTime")]
    public DateTime LoginTime { get; set; }

    [JsonProperty("expiresAt")]
    public DateTime ExpiresAt { get; set; }

    [JsonProperty("isActive")]
    public bool IsActive { get; set; } = true;
}
