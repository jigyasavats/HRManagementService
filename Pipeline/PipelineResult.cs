namespace HRManagementService.Pipeline
{
    public class PipelineResult
    {
        public List<PipelineStep> Steps { get; set; } = new();
        public bool IsFullyCompleted => Steps.All(s => s.Status == "Done");
        public bool HasFailures => Steps.Any(s => s.Status == "Failed");

        public void PrintSummary()
        {
            Console.WriteLine("\n--- Pipeline Summary ---");
            for (int i = 0; i < Steps.Count; i++)
            {
                var step = Steps[i];
                var icon = step.Status == "Done" ? "[OK]" : "[FAIL]";
                Console.WriteLine($"  Step {i + 1}/{Steps.Count}: {step.Name} {icon}");
                if (step.Status == "Failed")
                {
                    Console.WriteLine($"    Error: {step.ErrorMessage}");
                }
            }

            if (IsFullyCompleted)
                Console.WriteLine("\nAll steps completed successfully.");
            else
                Console.WriteLine("\nSome steps failed. Check details above.");
        }
    }
}
