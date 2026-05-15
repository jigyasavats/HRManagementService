using Newtonsoft.Json;

namespace HRManagementService.Models
{
    public class PromotionRequest
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("employeeId")]
        public string EmployeeId { get; set; } = string.Empty;

        [JsonProperty("alias")]
        public string Alias { get; set; } = string.Empty;

        [JsonProperty("employeeName")]
        public string EmployeeName { get; set; } = string.Empty;

        [JsonProperty("currentLevel")]
        public string CurrentLevel { get; set; } = string.Empty;

        [JsonProperty("currentSalary")]
        public decimal CurrentSalary { get; set; }

        [JsonProperty("proposedBy")]
        public string ProposedBy { get; set; } = string.Empty;

        [JsonProperty("justification")]
        public string Justification { get; set; } = string.Empty;

        [JsonProperty("proposedOn")]
        public DateTime ProposedOn { get; set; }

        [JsonProperty("newLevel")]
        public string NewLevel { get; set; } = string.Empty;

        [JsonProperty("newSalary")]
        public decimal NewSalary { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; } = "Pending";

        [JsonProperty("reviewedBy")]
        public string ReviewedBy { get; set; } = string.Empty;

        [JsonProperty("reviewedOn")]
        public DateTime? ReviewedOn { get; set; }

        [JsonProperty("hrComments")]
        public string HRComments { get; set; } = string.Empty;
    }
}
