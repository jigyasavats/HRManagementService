using Newtonsoft.Json;
using HRManagementService.Enums;

namespace HRManagementService.Models
{
    public class AuthUser
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("employeeId")]
        public string EmployeeId { get; set; } = string.Empty;

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("email")]
        public string Email { get; set; } = string.Empty;

        [JsonProperty("alias")]
        public string Alias { get; set; } = string.Empty;

        [JsonProperty("role")]
        public UserRole Role { get; set; }

        [JsonProperty("isActive")]
        public bool IsActive { get; set; } = true;
    }
}
