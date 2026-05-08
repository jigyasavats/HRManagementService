using Newtonsoft.Json;

namespace HRManagementService.Models
{
    public class HolidayConfig
    {
        [JsonProperty("id")]
        public string Id { get; set; } = "holiday-config";

        [JsonProperty("fixedHolidays")]
        public List<FixedHoliday> FixedHolidays { get; set; } = new();

        [JsonProperty("annualLeaveCount")]
        public int AnnualLeaveCount { get; set; }
    }

    public class FixedHoliday
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("date")]
        public DateTime Date { get; set; }
    }
}
