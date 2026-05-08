using Newtonsoft.Json;

namespace HRManagementService.Models
{
    public class OnboardingStatus
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("employeeId")]
        public string EmployeeId { get; set; } = string.Empty;

        [JsonProperty("employeeName")]
        public string EmployeeName { get; set; } = string.Empty;

        [JsonProperty("startedAt")]
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        [JsonProperty("completedAt")]
        public DateTime? CompletedAt { get; set; }

        [JsonProperty("overallStatus")]
        public string OverallStatus { get; set; } = "InProgress";

        [JsonProperty("steps")]
        public List<OnboardingStepStatus> Steps { get; set; } = new();
    }

    public class OnboardingStepStatus
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("status")]
        public string Status { get; set; } = "Pending";

        [JsonProperty("errorMessage")]
        public string? ErrorMessage { get; set; }

        [JsonProperty("completedAt")]
        public DateTime? CompletedAt { get; set; }
    }
}
