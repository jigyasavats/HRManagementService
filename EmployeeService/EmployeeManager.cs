using HRManagementService.Models;
using HRManagementService.Pipeline;
using HRManagementService.Repository;
using HRManagementService.Enums;

namespace HRManagementService.EmployeeService
{
    public class EmployeeManager
    {
        private readonly TeamRepository _teamRepo;
        private readonly PayrollRepository _payrollRepo;
        private readonly HolidayRepository _holidayRepo;
        private readonly OnboardingRepository _onboardingRepo;
        private readonly EmployeeRepository _employeeRepo;
        private readonly AuthRepository _authRepo;
        private readonly EmployeePipeline _pipeline;

        public EmployeeManager(
            TeamRepository teamRepo,
            PayrollRepository payrollRepo,
            HolidayRepository holidayRepo,
            OnboardingRepository onboardingRepo,
            EmployeeRepository employeeRepo,
            AuthRepository authRepo,
            EmployeePipeline pipeline)
        {
            _teamRepo = teamRepo;
            _payrollRepo = payrollRepo;
            _holidayRepo = holidayRepo;
            _onboardingRepo = onboardingRepo;
            _employeeRepo = employeeRepo;
            _authRepo = authRepo;
            _pipeline = pipeline;
        }

        public async Task AddNewEmployeeAsync(AuthUser currentUser)
        {
            Console.WriteLine("\n========================================");
            Console.WriteLine("   Add New Employee");
            Console.WriteLine("========================================\n");

            Console.Write("Employee Name: ");
            var name = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                Console.WriteLine("Name is required.");
                return;
            }

            Console.Write("Email: ");
            var email = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(email))
            {
                Console.WriteLine("Email is required.");
                return;
            }

            Console.Write("Alias: ");
            var alias = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(alias))
            {
                Console.WriteLine("Alias is required.");
                return;
            }

            var teams = await _teamRepo.GetAllTeamsAsync();
            if (teams.Count == 0)
            {
                Console.WriteLine("No teams exist. Please create a team first (Menu Option 6).");
                return;
            }

            Console.WriteLine("\nAvailable Teams:");
            for (int i = 0; i < teams.Count; i++)
            {
                Console.WriteLine($"  {i + 1}. {teams[i].TeamName} ({teams[i].TeamId})");
            }
            Console.Write("Select team number: ");
            if (!int.TryParse(Console.ReadLine()?.Trim(), out int teamIndex) || teamIndex < 1 || teamIndex > teams.Count)
            {
                Console.WriteLine("Invalid selection.");
                return;
            }
            var selectedTeam = teams[teamIndex - 1];

            var levels = await _payrollRepo.GetAllLevelsAsync();
            if (levels.Count == 0)
            {
                Console.WriteLine("No salary levels configured. Please setup salary levels first (Menu Option 4).");
                return;
            }

            Console.WriteLine("\nAvailable Levels:");
            for (int i = 0; i < levels.Count; i++)
            {
                Console.WriteLine($"  {i + 1}. {levels[i].Level} (${levels[i].MinSalary:N0} - ${levels[i].MaxSalary:N0})");
            }
            Console.Write("Select level number: ");
            if (!int.TryParse(Console.ReadLine()?.Trim(), out int levelIndex) || levelIndex < 1 || levelIndex > levels.Count)
            {
                Console.WriteLine("Invalid selection.");
                return;
            }
            var selectedLevel = levels[levelIndex - 1];

            Console.Write($"Salary (${selectedLevel.MinSalary:N0} - ${selectedLevel.MaxSalary:N0}): $");
            if (!decimal.TryParse(Console.ReadLine()?.Trim(), out decimal salary) ||
                salary < selectedLevel.MinSalary || salary > selectedLevel.MaxSalary)
            {
                Console.WriteLine($"Salary must be between ${selectedLevel.MinSalary:N0} and ${selectedLevel.MaxSalary:N0}.");
                return;
            }

            Console.WriteLine("\nRole:");
            Console.WriteLine("  1. Employee");
            Console.WriteLine("  2. Manager");
            Console.Write("Select role: ");
            var roleInput = Console.ReadLine()?.Trim();
            var role = roleInput == "2" ? UserRole.Manager : UserRole.Employee;

            var holidayConfig = await _holidayRepo.GetConfigAsync();
            int annualLeave = holidayConfig?.AnnualLeaveCount ?? 20;

            Console.WriteLine("\n--- Confirm Details ---");
            Console.WriteLine($"  Name:   {name}");
            Console.WriteLine($"  Email:  {email}");
            Console.WriteLine($"  Alias:  {alias}");
            Console.WriteLine($"  Team:   {selectedTeam.TeamName}");
            Console.WriteLine($"  Level:  {selectedLevel.Level}");
            Console.WriteLine($"  Salary: ${salary:N0}");
            Console.WriteLine($"  Role:   {role}");
            Console.WriteLine($"  Annual Leave: {annualLeave} days");
            Console.Write("\nProceed? (y/n): ");

            if (Console.ReadLine()?.Trim().ToLower() != "y")
            {
                Console.WriteLine("Cancelled.");
                return;
            }

            var onboardingData = new OnboardingEvent
            {
                Name = name,
                Email = email,
                Alias = alias,
                TeamId = selectedTeam.TeamId,
                Level = selectedLevel.Level,
                Salary = salary,
                Role = role,
                AnnualLeaveCount = annualLeave
            };

            Console.WriteLine("\nStarting onboarding pipeline...");
            var onboardingId = await _pipeline.StartAsync(onboardingData, currentUser.EmployeeId, currentUser.Role.ToString());

            if (onboardingId != null)
            {
                Console.WriteLine($"\nOnboarding process started! (ID: {onboardingId})");
                Console.WriteLine("Remaining steps are running in the background via Event Hub.");
                Console.WriteLine("Use 'Check Onboarding Status' to track progress.");
            }
            else
            {
                Console.WriteLine("\nFailed to start onboarding. Employee record could not be created.");
            }
        }

        public async Task CheckOnboardingStatusAsync()
        {
            Console.WriteLine("\n========================================");
            Console.WriteLine("   Check Onboarding Status");
            Console.WriteLine("========================================\n");

            Console.WriteLine("  1. View all in-progress onboardings");
            Console.WriteLine("  2. Search by employee name");
            Console.Write("\nChoice: ");
            var choice = Console.ReadLine()?.Trim();

            List<OnboardingStatus> results;

            if (choice == "2")
            {
                Console.Write("Enter employee name (or part of it): ");
                var searchName = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(searchName))
                {
                    Console.WriteLine("Name is required.");
                    return;
                }

                var all = await _onboardingRepo.GetAllAsync();
                results = all.Where(s => s.EmployeeName.Contains(searchName, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            else
            {
                results = await _onboardingRepo.GetInProgressAsync();
            }

            if (results.Count == 0)
            {
                Console.WriteLine("\nNo onboarding records found.");
                return;
            }

            Console.WriteLine($"\n--- Results ({results.Count}) ---\n");
            foreach (var status in results)
            {
                var icon = status.OverallStatus switch
                {
                    "Completed" => "[DONE]",
                    "InProgress" => "[IN PROGRESS]",
                    "CompletedWithErrors" => "[ERRORS]",
                    "Failed" => "[FAILED]",
                    _ => "[UNKNOWN]"
                };

                Console.WriteLine($"  {status.EmployeeName} (Employee ID: {status.EmployeeId}) — {icon}");
                Console.WriteLine($"    Onboarding ID: {status.Id}");
                Console.WriteLine($"    Started: {status.StartedAt:yyyy-MM-dd HH:mm:ss} UTC");
                if (status.CompletedAt.HasValue)
                    Console.WriteLine($"    Completed: {status.CompletedAt:yyyy-MM-dd HH:mm:ss} UTC");

                foreach (var step in status.Steps)
                {
                    var stepIcon = step.Status switch
                    {
                        "Done" => "[OK]",
                        "Running" => "[...]",
                        "Pending" => "[--]",
                        "Failed" => "[FAIL]",
                        _ => "[??]"
                    };
                    Console.Write($"      {step.Name}: {stepIcon}");
                    if (step.Status == "Failed")
                        Console.Write($" — {step.ErrorMessage}");
                    Console.WriteLine();
                }
                Console.WriteLine();
            }
        }

        public async Task UpdatePersonalInfoAsync(AuthUser currentUser)
        {
            Console.WriteLine("\n========================================");
            Console.WriteLine("   Update Personal Info");
            Console.WriteLine("========================================\n");

            var employee = await _employeeRepo.GetByAliasAsync(currentUser.Alias);
            if (employee == null)
            {
                Console.WriteLine("Employee record not found.");
                return;
            }

            Console.WriteLine($"  Name:         {employee.Name}");
            Console.WriteLine($"  Email:        {employee.Email}");
            Console.WriteLine($"  Alias:        {employee.Alias}");
            Console.WriteLine($"  Team:         {employee.TeamId}");
            Console.WriteLine($"  Joining Date: {employee.JoiningDate:yyyy-MM-dd}");

            Console.WriteLine("\nWhat do you want to update?");
            Console.WriteLine("  1. Name");
            Console.WriteLine("  2. Cancel");
            Console.Write("\nChoice: ");
            var choice = Console.ReadLine()?.Trim();

            if (choice == "1")
            {
                Console.Write($"New Name (current: {employee.Name}): ");
                var newName = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(newName))
                {
                    Console.WriteLine("Name is required.");
                    return;
                }
                employee.Name = newName;
                await _employeeRepo.UpdateEmployeeAsync(employee);

                // Sync AuthUser name
                currentUser.Name = newName;
                await _authRepo.UpdateUserAsync(currentUser);

                Console.WriteLine($"Name updated to '{newName}'.");
            }
            else
            {
                Console.WriteLine("Cancelled.");
            }
        }
    }
}
