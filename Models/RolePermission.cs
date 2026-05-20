using Newtonsoft.Json;

namespace HRManagementService.Models;

public class RolePermission
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("role")]
    public string Role { get; set; } = string.Empty;

    [JsonProperty("permissions")]
    public List<string> Permissions { get; set; } = new();
}
