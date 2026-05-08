using Newtonsoft.Json;

namespace HRManagementService.Models
{
    public class Team
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("teamId")]
        public string TeamId { get; set; } = string.Empty;

        [JsonProperty("teamName")]
        public string TeamName { get; set; } = string.Empty;

        [JsonProperty("managerId")]
        public string ManagerId { get; set; } = string.Empty;

        [JsonProperty("skipManagerId")]
        public string SkipManagerId { get; set; } = string.Empty;

        [JsonProperty("employeeIds")]
        public List<string> EmployeeIds { get; set; } = new();

        [JsonProperty("budget")]
        public decimal Budget { get; set; }
    }
}
