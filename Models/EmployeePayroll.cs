using Newtonsoft.Json;

namespace HRManagementService.Models
{
    public class EmployeePayroll
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("employeeId")]
        public string EmployeeId { get; set; } = string.Empty;

        [JsonProperty("alias")]
        public string Alias { get; set; } = string.Empty;

        [JsonProperty("level")]
        public string Level { get; set; } = string.Empty;

        [JsonProperty("salary")]
        public decimal Salary { get; set; }

        [JsonProperty("lastUpdated")]
        public DateTime LastUpdated { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; } = "Active";
    }
}
