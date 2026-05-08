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
        public List<ReviewEntry> Reviews { get; set; } = new();

        [JsonProperty("flaggedForTermination")]
        public bool FlaggedForTermination { get; set; } = false;

        [JsonProperty("flaggedBy")]
        public string FlaggedBy { get; set; } = string.Empty;

        [JsonProperty("flagReason")]
        public string FlagReason { get; set; } = string.Empty;

        [JsonProperty("flagDate")]
        public DateTime? FlagDate { get; set; }
    }

    public class ReviewEntry
    {
        [JsonProperty("reviewId")]
        public string ReviewId { get; set; } = Guid.NewGuid().ToString();

        [JsonProperty("reviewedBy")]
        public string ReviewedBy { get; set; } = string.Empty;

        [JsonProperty("reviewerRole")]
        public string ReviewerRole { get; set; } = string.Empty;

        [JsonProperty("rating")]
        public int Rating { get; set; }

        [JsonProperty("comments")]
        public string Comments { get; set; } = string.Empty;

        [JsonProperty("reviewDate")]
        public DateTime ReviewDate { get; set; } = DateTime.UtcNow;
    }
}
