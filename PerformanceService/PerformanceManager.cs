using HRManagementService.Models;
using HRManagementService.Repository;

namespace HRManagementService.PerformanceService
{
    public class PerformanceManager
    {
        private readonly PerformanceRepository _perfRepo;
        private readonly TeamRepository _teamRepo;
        private readonly AuthRepository _authRepo;

        public PerformanceManager(PerformanceRepository perfRepo, TeamRepository teamRepo, AuthRepository authRepo)
        {
            _perfRepo = perfRepo;
            _teamRepo = teamRepo;
            _authRepo = authRepo;
        }

        public async Task SubmitOwnReviewAsync(AuthUser currentUser)
        {
            Console.WriteLine("\n========================================");
            Console.WriteLine("   Submit Performance Review");
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
            var existingThisYear = perfRecord.Reviews.FirstOrDefault(r => r.Year == currentYear);

            if (existingThisYear != null)
            {
                Console.WriteLine($"  You have already submitted your review for {currentYear}.");
                Console.WriteLine($"  Submitted on: {existingThisYear.SubmittedOn:yyyy-MM-dd}");
                Console.WriteLine($"  Status: {existingThisYear.Status}");
                if (existingThisYear.Status == "Reviewed")
                {
                    Console.WriteLine($"  Manager Rating: {existingThisYear.ManagerRating}/5");
                    Console.WriteLine($"  Manager Comment: {existingThisYear.ManagerComment}");
                }
                return;
            }

            Console.WriteLine($"  Submitting review for year: {currentYear}\n");

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

            // Self-rating
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

            var ratingLabel = GetRatingLabel(rating);

            // Confirm
            Console.WriteLine($"\n  --- Review Summary ({currentYear}) ---");
            Console.WriteLine($"  Accomplishments: {accomplishments}");
            Console.WriteLine($"  Improvements:    {improvements}");
            Console.WriteLine($"  Goals:           {goals}");
            Console.WriteLine($"  Self-Rating:     {rating}/5 ({ratingLabel})");
            Console.Write("\n  Submit? (y/n): ");

            if (Console.ReadLine()?.Trim().ToLower() != "y")
            {
                Console.WriteLine("  Cancelled.");
                return;
            }

            var review = new YearlyReview
            {
                ReviewId = Guid.NewGuid().ToString(),
                Year = currentYear,
                Accomplishments = accomplishments,
                Improvements = improvements,
                Goals = goals,
                EmployeeRating = rating,
                SubmittedOn = DateTime.UtcNow,
                Status = "Pending Review"
            };

            perfRecord.Reviews.Add(review);
            await _perfRepo.UpdatePerformanceAsync(perfRecord);

            Console.WriteLine($"\n  Review for {currentYear} submitted! Status: Pending Review");
        }

        public async Task CheckOwnHistoryAsync(AuthUser currentUser)
        {
            Console.WriteLine("\n========================================");
            Console.WriteLine("   My Performance History");
            Console.WriteLine("========================================\n");

            var perfRecord = await _perfRepo.GetByAliasAsync(currentUser.Alias);
            if (perfRecord == null || perfRecord.Reviews.Count == 0)
            {
                Console.WriteLine("  No performance reviews found.");
                return;
            }

            foreach (var review in perfRecord.Reviews.OrderByDescending(r => r.Year))
            {
                Console.WriteLine($"  --- {review.Year} ---");
                Console.WriteLine($"  Status:          {review.Status}");
                Console.WriteLine($"  Accomplishments: {review.Accomplishments}");
                Console.WriteLine($"  Improvements:    {review.Improvements}");
                Console.WriteLine($"  Goals:           {review.Goals}");
                Console.WriteLine($"  Self-Rating:     {review.EmployeeRating}/5 ({GetRatingLabel(review.EmployeeRating)})");
                Console.WriteLine($"  Submitted:       {review.SubmittedOn:yyyy-MM-dd}");

                if (review.Status == "Reviewed")
                {
                    Console.WriteLine($"  Manager Rating:  {review.ManagerRating}/5 ({GetRatingLabel(review.ManagerRating)})");
                    Console.WriteLine($"  Manager Comment: {review.ManagerComment}");
                    Console.WriteLine($"  Reviewed By:     {review.ReviewedBy}");
                    Console.WriteLine($"  Reviewed On:     {review.ReviewedOn:yyyy-MM-dd}");
                }
                Console.WriteLine();
            }
        }

        public async Task ReviewTeamPerformanceAsync(AuthUser currentUser)
        {
            Console.WriteLine("\n========================================");
            Console.WriteLine("   Team Performance Review");
            Console.WriteLine("========================================\n");

            // Get manager's teams
            var allTeams = await _teamRepo.GetAllTeamsAsync();
            var myTeams = allTeams.Where(t => t.ManagerId == currentUser.Alias).ToList();

            if (myTeams.Count == 0)
            {
                Console.WriteLine("  You don't manage any teams.");
                return;
            }

            // Collect all team member aliases
            var memberAliases = myTeams.SelectMany(t => t.EmployeeIds).Distinct().ToList();

            if (memberAliases.Count == 0)
            {
                Console.WriteLine("  No team members found.");
                return;
            }

            // Find members with pending reviews
            var pendingMembers = new List<(string alias, YearlyReview review, PerformanceReview record)>();
            foreach (var alias in memberAliases)
            {
                var perfRecord = await _perfRepo.GetByAliasAsync(alias);
                if (perfRecord == null) continue;

                var pending = perfRecord.Reviews.Where(r => r.Status == "Pending Review").ToList();
                foreach (var review in pending)
                {
                    pendingMembers.Add((alias, review, perfRecord));
                }
            }

            if (pendingMembers.Count == 0)
            {
                Console.WriteLine("  No pending reviews to evaluate.");
                return;
            }

            Console.WriteLine($"  Pending Reviews ({pendingMembers.Count}):\n");
            for (int i = 0; i < pendingMembers.Count; i++)
            {
                var (alias, review, _) = pendingMembers[i];
                Console.WriteLine($"    {i + 1}. {alias} — Year: {review.Year} | Self-Rating: {review.EmployeeRating}/5");
            }
            Console.Write("\n  Select to review (0 to cancel): ");

            if (!int.TryParse(Console.ReadLine()?.Trim(), out int sel) || sel < 0 || sel > pendingMembers.Count)
            {
                Console.WriteLine("  Invalid selection.");
                return;
            }
            if (sel == 0) return;

            var selected = pendingMembers[sel - 1];

            // Show employee's review
            Console.WriteLine($"\n  --- {selected.alias}'s Review ({selected.review.Year}) ---");
            Console.WriteLine($"  Accomplishments: {selected.review.Accomplishments}");
            Console.WriteLine($"  Improvements:    {selected.review.Improvements}");
            Console.WriteLine($"  Goals:           {selected.review.Goals}");
            Console.WriteLine($"  Self-Rating:     {selected.review.EmployeeRating}/5 ({GetRatingLabel(selected.review.EmployeeRating)})");
            Console.WriteLine($"  Submitted:       {selected.review.SubmittedOn:yyyy-MM-dd}");

            // Manager adds comment
            Console.WriteLine("\n  Add your comment for this employee:");
            Console.Write("  > ");
            var managerComment = Console.ReadLine()?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(managerComment))
            {
                Console.WriteLine("  Comment cannot be empty.");
                return;
            }

            // Manager gives rating
            Console.WriteLine("\n  Rate this employee (1-5):");
            Console.WriteLine("    1 - Needs Improvement");
            Console.WriteLine("    2 - Below Expectations");
            Console.WriteLine("    3 - Meets Expectations");
            Console.WriteLine("    4 - Exceeds Expectations");
            Console.WriteLine("    5 - Outstanding");
            Console.Write("\n  Your Rating: ");

            if (!int.TryParse(Console.ReadLine()?.Trim(), out int managerRating) || managerRating < 1 || managerRating > 5)
            {
                Console.WriteLine("  Invalid rating.");
                return;
            }

            // Confirm
            Console.WriteLine($"\n  --- Confirm ---");
            Console.WriteLine($"  Employee:       {selected.alias}");
            Console.WriteLine($"  Your Comment:   {managerComment}");
            Console.WriteLine($"  Your Rating:    {managerRating}/5 ({GetRatingLabel(managerRating)})");
            Console.Write("\n  Submit review? (y/n): ");

            if (Console.ReadLine()?.Trim().ToLower() != "y")
            {
                Console.WriteLine("  Cancelled.");
                return;
            }

            // Update the review
            selected.review.ManagerComment = managerComment;
            selected.review.ManagerRating = managerRating;
            selected.review.ReviewedBy = currentUser.Alias;
            selected.review.ReviewedOn = DateTime.UtcNow;
            selected.review.Status = "Reviewed";

            await _perfRepo.UpdatePerformanceAsync(selected.record);

            Console.WriteLine($"\n  Review submitted! {selected.alias}'s {selected.review.Year} review marked as Reviewed.");
        }

        private static string GetRatingLabel(int rating) => rating switch
        {
            1 => "Needs Improvement",
            2 => "Below Expectations",
            3 => "Meets Expectations",
            4 => "Exceeds Expectations",
            5 => "Outstanding",
            _ => "Unknown"
        };
    }
}
