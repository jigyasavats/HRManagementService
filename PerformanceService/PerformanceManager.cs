using HRManagementService.Models;
using HRManagementService.Repository;

namespace HRManagementService.PerformanceService
{
    public class PerformanceManager
    {
        private readonly PerformanceRepository _perfRepo;

        public PerformanceManager(PerformanceRepository perfRepo)
        {
            _perfRepo = perfRepo;
        }

        public async Task SubmitOwnReviewAsync(AuthUser currentUser)
        {
            Console.WriteLine("\n========================================");
            Console.WriteLine("   Submit Own Performance Review");
            Console.WriteLine("========================================\n");

            var perfRecord = await _perfRepo.GetByAliasAsync(currentUser.Alias);
            if (perfRecord == null)
            {
                perfRecord = new PerformanceReview
                {
                    Id = Guid.NewGuid().ToString(),
                    Alias = currentUser.Alias
                };
                await _perfRepo.CreatePerformanceRecordAsync(perfRecord);
            }

            int currentYear = DateTime.UtcNow.Year;
            var existingThisYear = perfRecord.Reviews
                .FirstOrDefault(r => r.ReviewerRole == "Self" && r.ReviewDate.Year == currentYear);

            if (existingThisYear != null)
            {
                Console.WriteLine($"  You have already submitted your self-review for {currentYear}.");
                Console.WriteLine($"  Submitted on: {existingThisYear.ReviewDate:yyyy-MM-dd}");
                Console.WriteLine($"  Rating: {existingThisYear.Rating}/5");
                Console.WriteLine("\n  You can only submit one self-review per year.");
                return;
            }

            // Show previous years' reviews
            var pastReviews = perfRecord.Reviews
                .Where(r => r.ReviewerRole == "Self")
                .OrderByDescending(r => r.ReviewDate).ToList();

            if (pastReviews.Count > 0)
            {
                Console.WriteLine("  --- Past Self-Reviews ---");
                foreach (var past in pastReviews)
                {
                    Console.WriteLine($"    {past.ReviewDate.Year} | Rating: {past.Rating}/5 | Submitted: {past.ReviewDate:yyyy-MM-dd}");
                }
                Console.WriteLine();
            }

            Console.WriteLine($"  Submitting self-review for year: {currentYear}\n");

            // Accomplishments
            Console.WriteLine("  What were your key accomplishments this year?");
            Console.Write("  > ");
            var accomplishments = Console.ReadLine()?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(accomplishments))
            {
                Console.WriteLine("  Accomplishments cannot be empty.");
                return;
            }

            // Improvements
            Console.WriteLine("\n  What areas do you want to improve?");
            Console.Write("  > ");
            var improvements = Console.ReadLine()?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(improvements))
            {
                Console.WriteLine("  Improvements cannot be empty.");
                return;
            }

            // Goals
            Console.WriteLine("\n  What are your goals for next year?");
            Console.Write("  > ");
            var goals = Console.ReadLine()?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(goals))
            {
                Console.WriteLine("  Goals cannot be empty.");
                return;
            }

            // Overall self-rating
            Console.WriteLine("\n  Rate yourself overall (1-5):");
            Console.WriteLine("    1 - Needs Improvement");
            Console.WriteLine("    2 - Below Expectations");
            Console.WriteLine("    3 - Meets Expectations");
            Console.WriteLine("    4 - Exceeds Expectations");
            Console.WriteLine("    5 - Outstanding");
            Console.Write("\n  Your Rating: ");

            if (!int.TryParse(Console.ReadLine()?.Trim(), out int rating) || rating < 1 || rating > 5)
            {
                Console.WriteLine("  Invalid rating.");
                return;
            }

            var ratingLabel = rating switch
            {
                1 => "Needs Improvement",
                2 => "Below Expectations",
                3 => "Meets Expectations",
                4 => "Exceeds Expectations",
                5 => "Outstanding",
                _ => "Unknown"
            };

            // Confirm
            Console.WriteLine($"\n  --- Review Summary ({currentYear}) ---");
            Console.WriteLine($"  Accomplishments: {accomplishments}");
            Console.WriteLine($"  Improvements:    {improvements}");
            Console.WriteLine($"  Goals:           {goals}");
            Console.WriteLine($"  Overall Rating:  {rating}/5 ({ratingLabel})");
            Console.Write("\n  Submit? (y/n): ");

            if (Console.ReadLine()?.Trim().ToLower() != "y")
            {
                Console.WriteLine("  Cancelled.");
                return;
            }

            var comments = $"Accomplishments: {accomplishments} | Improvements: {improvements} | Goals: {goals}";

            var review = new ReviewEntry
            {
                ReviewId = Guid.NewGuid().ToString(),
                ReviewedBy = currentUser.Alias,
                ReviewerRole = "Self",
                Rating = rating,
                Comments = comments,
                ReviewDate = DateTime.UtcNow
            };

            perfRecord.Reviews.Add(review);
            await _perfRepo.UpdatePerformanceAsync(perfRecord);

            Console.WriteLine($"\n  Self-review for {currentYear} submitted successfully!");
        }
    }
}
