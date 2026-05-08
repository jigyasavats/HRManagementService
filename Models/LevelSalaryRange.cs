using Newtonsoft.Json;

namespace HRManagementService.Models
{
    public class LevelSalaryRange
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("level")]
        public string Level { get; set; } = string.Empty;

        [JsonProperty("minSalary")]
        public decimal MinSalary { get; set; }

        [JsonProperty("maxSalary")]
        public decimal MaxSalary { get; set; }
    }
}
