using Newtonsoft.Json;
using HRManagementService.Enums;

namespace HRManagementService.Pipeline
{
    public class OnboardingEvent
    {
        [JsonProperty("employeeId")]
        public string EmployeeId { get; set; } = string.Empty;

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("email")]
        public string Email { get; set; } = string.Empty;

        [JsonProperty("alias")]
        public string Alias { get; set; } = string.Empty;

        [JsonProperty("teamId")]
        public string TeamId { get; set; } = string.Empty;

        [JsonProperty("level")]
        public string Level { get; set; } = string.Empty;

        [JsonProperty("salary")]
        public decimal Salary { get; set; }

        [JsonProperty("role")]
        public UserRole Role { get; set; } = UserRole.Employee;

        [JsonProperty("annualLeaveCount")]
        public int AnnualLeaveCount { get; set; }
    }
}
