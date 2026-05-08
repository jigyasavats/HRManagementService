namespace HRManagementService.Pipeline
{
    public class PipelineStep
    {
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public string? ErrorMessage { get; set; }

        public void MarkDone()
        {
            Status = "Done";
        }

        public void MarkFailed(string error)
        {
            Status = "Failed";
            ErrorMessage = error;
        }
    }
}
