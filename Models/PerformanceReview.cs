using Newtonsoft.Json;

namespace HRManagementService.Models
{
    public class PerformanceReview
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("alias")]
        public string Alias { get; set; } = string.Empty;

        [JsonProperty("reviews")]
        public List<YearlyReview> Reviews { get; set; } = new();
    }

    public class YearlyReview
    {
        [JsonProperty("reviewId")]
        public string ReviewId { get; set; } = Guid.NewGuid().ToString();

        [JsonProperty("year")]
        public int Year { get; set; }

        // Employee fills these
        [JsonProperty("accomplishments")]
        public string Accomplishments { get; set; } = string.Empty;

        [JsonProperty("improvements")]
        public string Improvements { get; set; } = string.Empty;

        [JsonProperty("goals")]
        public string Goals { get; set; } = string.Empty;

        [JsonProperty("employeeRating")]
        public int EmployeeRating { get; set; }

        [JsonProperty("submittedOn")]
        public DateTime SubmittedOn { get; set; }

        // Manager fills these
        [JsonProperty("managerComment")]
        public string ManagerComment { get; set; } = string.Empty;

        [JsonProperty("managerRating")]
        public int ManagerRating { get; set; }

        [JsonProperty("reviewedBy")]
        public string ReviewedBy { get; set; } = string.Empty;

        [JsonProperty("reviewedOn")]
        public DateTime? ReviewedOn { get; set; }

        // Status: "Pending Review" | "Reviewed"
        [JsonProperty("status")]
        public string Status { get; set; } = "Pending Review";
    }
}
