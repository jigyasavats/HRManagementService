using Newtonsoft.Json;

namespace HRManagementService.Models
{
    public class Employee
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("email")]
        public string Email { get; set; } = string.Empty;

        [JsonProperty("alias")]
        public string Alias { get; set; } = string.Empty;

        [JsonProperty("teamId")]
        public string TeamId { get; set; } = string.Empty;

        [JsonProperty("joiningDate")]
        public DateTime JoiningDate { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; } = "Active";

        [JsonProperty("terminatedOn")]
        public DateTime? TerminatedOn { get; set; }
    }
}
