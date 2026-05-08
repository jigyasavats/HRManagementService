using Newtonsoft.Json;

namespace HRManagementService.Models
{
    public class EmployeeHolidayBank
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("employeeId")]
        public string EmployeeId { get; set; } = string.Empty;

        [JsonProperty("annualLeaveBalance")]
        public int AnnualLeaveBalance { get; set; }

        [JsonProperty("requests")]
        public List<HolidayRequest> Requests { get; set; } = new();
    }

    public class HolidayRequest
    {
        [JsonProperty("requestId")]
        public string RequestId { get; set; } = Guid.NewGuid().ToString();

        [JsonProperty("startDate")]
        public DateTime StartDate { get; set; }

        [JsonProperty("endDate")]
        public DateTime EndDate { get; set; }

        [JsonProperty("reason")]
        public string Reason { get; set; } = string.Empty;

        [JsonProperty("status")]
        public string Status { get; set; } = "Pending";

        [JsonProperty("employeeName")]
        public string EmployeeName { get; set; } = string.Empty;

        [JsonProperty("requestedOn")]
        public DateTime RequestedOn { get; set; } = DateTime.UtcNow;
    }
}
