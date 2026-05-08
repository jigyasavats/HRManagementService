using Newtonsoft.Json;

namespace HRManagementService.Models
{
    public class AuditLog
    {
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonProperty("action")]
        public string Action { get; set; } = string.Empty;

        [JsonProperty("performedBy")]
        public string PerformedBy { get; set; } = string.Empty;

        [JsonProperty("performedByRole")]
        public string PerformedByRole { get; set; } = string.Empty;

        [JsonProperty("targetEmployeeId")]
        public string TargetEmployeeId { get; set; } = string.Empty;

        [JsonProperty("details")]
        public string Details { get; set; } = string.Empty;

        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
