using Newtonsoft.Json;

namespace HRManagementService.Pipeline
{
    public class OffboardingEvent
    {
        [JsonProperty("employeeId")]
        public string EmployeeId { get; set; } = string.Empty;

        [JsonProperty("alias")]
        public string Alias { get; set; } = string.Empty;

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("teamId")]
        public string TeamId { get; set; } = string.Empty;

        [JsonProperty("isManager")]
        public bool IsManager { get; set; }
    }
}
