using HRManagementService.Models;
using HRManagementService.Pipeline;
using HRManagementService.Repository;
using HRManagementService.Enums;
using HRManagementService.AIService;

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
        private readonly PerformanceRepository _performanceRepo;
        private readonly PromotionRepository _promotionRepo;
        private readonly EmployeePipeline _pipeline;
        private readonly OffboardingPipeline _offboardingPipeline;
        private readonly ServiceBusService _serviceBus;
        private readonly AuditRepository _auditRepo;
        private readonly AIManager _aiManager;
        private readonly Dictionary<string, string> _queueNames;

        public EmployeeManager(
            TeamRepository teamRepo,
            PayrollRepository payrollRepo,
            HolidayRepository holidayRepo,
            OnboardingRepository onboardingRepo,
            EmployeeRepository employeeRepo,
            AuthRepository authRepo,
            PerformanceRepository performanceRepo,
            PromotionRepository promotionRepo,
            EmployeePipeline pipeline,
            OffboardingPipeline offboardingPipeline,
            ServiceBusService serviceBus,
            AuditRepository auditRepo,
            AIManager aiManager,
            Dictionary<string, string> queueNames)
        {
            _teamRepo = teamRepo;
            _payrollRepo = payrollRepo;
            _holidayRepo = holidayRepo;
            _onboardingRepo = onboardingRepo;
            _employeeRepo = employeeRepo;
            _authRepo = authRepo;
            _performanceRepo = performanceRepo;
            _promotionRepo = promotionRepo;
            _pipeline = pipeline;
            _offboardingPipeline = offboardingPipeline;
            _serviceBus = serviceBus;
            _auditRepo = auditRepo;
            _aiManager = aiManager;
            _queueNames = queueNames;
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
            Console.WriteLine("   Check Pipeline Status");
            Console.WriteLine("========================================\n");

            Console.WriteLine("  1. View all in-progress pipelines");
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
                Console.WriteLine("\nNo pipeline records found.");
                return;
            }

            Console.WriteLine($"\n--- Results ({results.Count}) ---\n");
            foreach (var status in results)
            {
                var isOffboarding = status.Steps.Any(s => s.Name == "Mark Terminated");
                var pipelineType = isOffboarding ? "OFFBOARDING" : "ONBOARDING";

                var icon = status.OverallStatus switch
                {
                    "Completed" => "[DONE]",
                    "InProgress" => "[IN PROGRESS]",
                    "CompletedWithErrors" => "[ERRORS]",
                    "Failed" => "[FAILED]",
                    _ => "[UNKNOWN]"
                };

                Console.WriteLine($"  [{pipelineType}] {status.EmployeeName} — {icon}");
                Console.WriteLine($"    Pipeline ID: {status.Id}");
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

        public async Task TerminateEmployeeAsync(AuthUser currentUser, Func<Permission, string, Task<bool>>? scopeChecker = null)
        {
            Console.WriteLine("\n========================================");
            Console.WriteLine("   Terminate Employee");
            Console.WriteLine("========================================\n");

            var allEmployees = await _employeeRepo.GetAllEmployeesAsync();
            var activeEmployees = allEmployees
                .Where(e => e.Status != "Terminated" && e.Alias != currentUser.Alias)
                .ToList();

            if (activeEmployees.Count == 0)
            {
                Console.WriteLine("No active employees to terminate.");
                return;
            }

            Console.WriteLine("  Select employee to terminate:\n");
            for (int i = 0; i < activeEmployees.Count; i++)
            {
                Console.WriteLine($"    {i + 1}. {activeEmployees[i].Name} ({activeEmployees[i].Alias})");
            }
            Console.Write("\n  Choice (0 to cancel): ");

            if (!int.TryParse(Console.ReadLine()?.Trim(), out int sel) || sel < 0 || sel > activeEmployees.Count)
            {
                Console.WriteLine("  Invalid selection.");
                return;
            }
            if (sel == 0) return;

            var employee = activeEmployees[sel - 1];

            if (scopeChecker != null && !await scopeChecker(Permission.TerminateEmployee, employee.Alias))
                return;

            // Check if this employee is a manager
            var allTeams = await _teamRepo.GetAllTeamsAsync();
            var isManager = allTeams.Any(t => t.ManagerId == employee.Alias);
            var managedTeam = allTeams.FirstOrDefault(t => t.ManagerId == employee.Alias);

            // Show summary
            Console.WriteLine($"\n  --- Employee Details ---");
            Console.WriteLine($"  Name:      {employee.Name}");
            Console.WriteLine($"  Alias:     {employee.Alias}");
            Console.WriteLine($"  Team:      {employee.TeamId}");
            Console.WriteLine($"  Role:      {(isManager ? "Manager" : "Employee")}");

            if (isManager && managedTeam != null)
            {
                Console.WriteLine($"\n  ⚠ WARNING: This employee manages team '{managedTeam.TeamName}'");
                Console.WriteLine($"  Team has {managedTeam.EmployeeIds.Count} member(s).");

                if (!string.IsNullOrEmpty(managedTeam.SkipManagerId))
                {
                    Console.WriteLine($"  → Members will be reassigned to skip manager: {managedTeam.SkipManagerId}");
                }
                else
                {
                    Console.WriteLine($"  → No skip manager set. Members will stay in team without a manager.");
                }
            }

            Console.Write("\n  Confirm termination? (yes/no): ");
            var confirm = Console.ReadLine()?.Trim().ToLower();
            if (confirm != "yes")
            {
                Console.WriteLine("  Termination cancelled.");
                return;
            }

            var offboardingData = new OffboardingEvent
            {
                EmployeeId = employee.Id,
                Alias = employee.Alias,
                Name = employee.Name,
                TeamId = employee.TeamId,
                IsManager = isManager
            };

            var statusId = await _offboardingPipeline.StartAsync(
                offboardingData, currentUser.Alias, currentUser.Role.ToString());

            if (statusId != null)
            {
                Console.WriteLine($"\n  Offboarding pipeline started. Status ID: {statusId}");
                Console.WriteLine("  Use 'Check Pipeline Status' to track progress.");
            }
        }

        public async Task ProposePromotionAsync(AuthUser currentUser, Func<Permission, string, Task<bool>>? scopeChecker = null)
        {
            Console.WriteLine("\n========================================");
            Console.WriteLine("   Propose Promotion");
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
            var memberAliases = myTeams.SelectMany(t => t.EmployeeIds).ToHashSet();
            var allEmployees = await _employeeRepo.GetAllEmployeesAsync();
            var teamMembers = allEmployees
                .Where(e => memberAliases.Contains(e.Alias) && e.Status != "Terminated")
                .ToList();

            if (teamMembers.Count == 0)
            {
                Console.WriteLine("  No active team members found.");
                return;
            }

            while (true)
            {
                Console.WriteLine("  Select employee to propose for promotion:\n");
                for (int i = 0; i < teamMembers.Count; i++)
                {
                    Console.WriteLine($"    {i + 1}. {teamMembers[i].Name} ({teamMembers[i].Alias})");
                }
                Console.Write("\n  Choice (0 to cancel): ");

                if (!int.TryParse(Console.ReadLine()?.Trim(), out int sel) || sel < 0 || sel > teamMembers.Count)
                {
                    Console.WriteLine("  Invalid selection.");
                    return;
                }
                if (sel == 0) return;

                var employee = teamMembers[sel - 1];

                if (scopeChecker != null && !await scopeChecker(Permission.ProposePromotion, employee.Alias))
                    continue;

                // Check for duplicate pending proposal
                var existingPending = await _promotionRepo.GetPendingByAliasAsync(employee.Alias);
                if (existingPending != null)
                {
                    Console.WriteLine($"\n  A promotion proposal for {employee.Name} is already pending (proposed by {existingPending.ProposedBy} on {existingPending.ProposedOn:yyyy-MM-dd}).");
                    continue;
                }

                // Get current payroll
                var payroll = await _payrollRepo.GetPayrollByEmployeeIdAsync(employee.Id);
                if (payroll == null)
                {
                    Console.WriteLine("  No payroll record found for this employee.");
                    continue;
                }

                // Get performance history
                var perfRecord = await _performanceRepo.GetByAliasAsync(employee.Alias);

                // Display performance summary
                Console.WriteLine($"\n  --- Employee Details ---");
                Console.WriteLine($"  Name:          {employee.Name} ({employee.Alias})");
                Console.WriteLine($"  Current Level: {payroll.Level}");
                Console.WriteLine($"  Current Salary: ${payroll.Salary:N0}");

                if (perfRecord != null && perfRecord.Reviews.Count > 0)
                {
                    Console.WriteLine($"\n  --- Performance History ---");
                    foreach (var review in perfRecord.Reviews.OrderByDescending(r => r.Year))
                    {
                        Console.WriteLine($"    Year {review.Year}: Self-Rating: {review.EmployeeRating}/5 | Manager-Rating: {(review.ManagerRating > 0 ? $"{review.ManagerRating}/5" : "Not reviewed")} | Status: {review.Status}");
                    }
                }
                else
                {
                    Console.WriteLine("\n  No performance reviews found.");
                }

                // Offer AI promotion advice
                string? justification = null;
                Console.Write("\n  Would you like AI to assess promotion readiness? (y/n): ");
                if (Console.ReadLine()?.Trim().ToLower() == "y")
                {
                    Console.WriteLine("\n  Analyzing with AI...\n");

                    var perfSummary = "No performance reviews on record.";
                    if (perfRecord != null && perfRecord.Reviews.Count > 0)
                    {
                        var reviewLines = perfRecord.Reviews.OrderByDescending(r => r.Year)
                            .Select(r => $"Year {r.Year}: Self={r.EmployeeRating}/5, Manager={( r.ManagerRating > 0 ? $"{r.ManagerRating}/5" : "N/A")}, Accomplishments: {r.Accomplishments}");
                        perfSummary = string.Join("\n", reviewLines);
                    }

                    var systemPrompt = "You are a promotion advisor for managers. Based on the employee's performance history, current level, and salary, assess whether they are ready for promotion. Provide a clear recommendation (Ready / Not Yet / Needs More Data) with a brief justification. Also suggest what the manager should write as justification if recommending. Return in this format:\nRecommendation: <Ready/Not Yet/Needs More Data>\nReason: <brief reason>\nSuggested Justification: <what manager can write>\nKeep each section under 2 sentences.";
                    var userPrompt = $"Employee: {employee.Name} ({employee.Alias})\nCurrent Level: {payroll.Level}\nCurrent Salary: ${payroll.Salary:N0}\nPerformance History:\n{perfSummary}";

                    var aiResponse = await _aiManager.GetCompletionAsync(systemPrompt, userPrompt);
                    Console.WriteLine("  --- AI Promotion Advice ---");
                    Console.WriteLine($"  {aiResponse}");
                    Console.WriteLine("  ----------------------------\n");

                    Console.WriteLine("  What would you like to do?");
                    Console.WriteLine("    1. Use AI suggested justification");
                    Console.WriteLine("    2. Write my own justification");
                    Console.WriteLine("    3. Skip — check another employee");
                    Console.Write("\n  Choice: ");

                    var aiChoice = Console.ReadLine()?.Trim();
                    if (aiChoice == "3")
                    {
                        Console.WriteLine("\n  Skipped. Returning to employee list.\n");
                        continue;
                    }
                    else if (aiChoice == "1")
                    {
                        var justIdx = aiResponse.IndexOf("Suggested Justification:", StringComparison.OrdinalIgnoreCase);
                        if (justIdx >= 0)
                        {
                            justification = aiResponse.Substring(justIdx + "Suggested Justification:".Length).Trim();
                            Console.WriteLine($"\n  Using AI justification: {justification}");
                        }
                        else
                        {
                            Console.WriteLine("  Could not parse AI justification. Please enter manually.");
                        }
                    }
                }

                // Manager enters justification (if not already set by AI)
                if (string.IsNullOrEmpty(justification))
                {
                    Console.Write("\n  Justification for promotion:\n  > ");
                    justification = Console.ReadLine()?.Trim();
                }

                if (string.IsNullOrEmpty(justification))
                {
                    Console.WriteLine("  Justification is required. Cancelled.");
                    return;
                }

                // Confirm
                Console.Write("\n  Submit promotion proposal? (yes/no): ");
                var confirm = Console.ReadLine()?.Trim().ToLower();
                if (confirm != "yes")
                {
                    Console.WriteLine("  Cancelled.");
                    return;
                }

                var request = new PromotionRequest
                {
                    Id = Guid.NewGuid().ToString(),
                    EmployeeId = employee.Id,
                    Alias = employee.Alias,
                    EmployeeName = employee.Name,
                    CurrentLevel = payroll.Level,
                    CurrentSalary = payroll.Salary,
                    ProposedBy = currentUser.Alias,
                    Justification = justification,
                    ProposedOn = DateTime.UtcNow
                };

                await _promotionRepo.CreateAsync(request);
                Console.WriteLine($"\n  Promotion proposal submitted for {employee.Name}. HR will review it.");
                return;
            }
        }

        public async Task ReviewPromotionAsync(AuthUser currentUser)
        {
            Console.WriteLine("\n========================================");
            Console.WriteLine("   Review Promotion Proposals");
            Console.WriteLine("========================================\n");

            Console.WriteLine("  1. Review Pending Proposals");
            Console.WriteLine("  2. View All Proposals (History)");
            Console.Write("\nChoice: ");
            var menuChoice = Console.ReadLine()?.Trim();

            List<PromotionRequest> proposals;
            if (menuChoice == "2")
            {
                proposals = await _promotionRepo.GetAllAsync();
                if (proposals.Count == 0)
                {
                    Console.WriteLine("\n  No promotion proposals found.");
                    return;
                }
                Console.WriteLine($"\n  --- All Proposals ({proposals.Count}) ---\n");
                foreach (var p in proposals.OrderByDescending(p => p.ProposedOn))
                {
                    var statusLabel = p.Status switch
                    {
                        "Approved" => "[APPROVED]",
                        "Rejected" => "[REJECTED]",
                        _ => "[PENDING]"
                    };
                    Console.WriteLine($"    {p.EmployeeName} ({p.Alias}) — {p.CurrentLevel} → {(p.NewLevel != "" ? p.NewLevel : "?")} — {statusLabel}");
                    Console.WriteLine($"      Proposed by: {p.ProposedBy} on {p.ProposedOn:yyyy-MM-dd}");
                    if (p.Status != "Pending")
                        Console.WriteLine($"      Reviewed by: {p.ReviewedBy} on {p.ReviewedOn:yyyy-MM-dd} — {p.HRComments}");
                    Console.WriteLine();
                }
                return;
            }

            // Review pending proposals
            proposals = await _promotionRepo.GetAllPendingAsync();
            if (proposals.Count == 0)
            {
                Console.WriteLine("\n  No pending promotion proposals.");
                return;
            }

            Console.WriteLine($"  Pending Proposals ({proposals.Count}):\n");
            for (int i = 0; i < proposals.Count; i++)
            {
                Console.WriteLine($"    {i + 1}. {proposals[i].EmployeeName} ({proposals[i].Alias}) — Level: {proposals[i].CurrentLevel} — by {proposals[i].ProposedBy}");
            }
            Console.Write("\n  Select proposal to review (0 to cancel): ");

            if (!int.TryParse(Console.ReadLine()?.Trim(), out int sel) || sel < 0 || sel > proposals.Count)
            {
                Console.WriteLine("  Invalid selection.");
                return;
            }
            if (sel == 0) return;

            var proposal = proposals[sel - 1];

            // Show full details
            Console.WriteLine($"\n  --- Proposal Details ---");
            Console.WriteLine($"  Employee:       {proposal.EmployeeName} ({proposal.Alias})");
            Console.WriteLine($"  Current Level:  {proposal.CurrentLevel}");
            Console.WriteLine($"  Current Salary: ${proposal.CurrentSalary:N0}");
            Console.WriteLine($"  Proposed By:    {proposal.ProposedBy}");
            Console.WriteLine($"  Proposed On:    {proposal.ProposedOn:yyyy-MM-dd}");
            Console.WriteLine($"  Justification:  {proposal.Justification}");

            // Show performance history
            var perfRecord = await _performanceRepo.GetByAliasAsync(proposal.Alias);
            if (perfRecord != null && perfRecord.Reviews.Count > 0)
            {
                Console.WriteLine($"\n  --- Performance History ---");
                foreach (var review in perfRecord.Reviews.OrderByDescending(r => r.Year))
                {
                    Console.WriteLine($"    Year {review.Year}: Self: {review.EmployeeRating}/5 | Manager: {(review.ManagerRating > 0 ? $"{review.ManagerRating}/5" : "N/A")} | Status: {review.Status}");
                }
            }

            // Show team budget
            var allTeams = await _teamRepo.GetAllTeamsAsync();
            var employeeTeam = allTeams.FirstOrDefault(t => t.EmployeeIds.Contains(proposal.Alias));
            if (employeeTeam != null)
            {
                Console.WriteLine($"\n  Team:           {employeeTeam.TeamName}");
                Console.WriteLine($"  Team Budget:    ${employeeTeam.Budget:N0}");
            }

            // Approve or Reject
            Console.Write("\n  Action (approve/reject): ");
            var action = Console.ReadLine()?.Trim().ToLower();

            if (action == "reject")
            {
                Console.Write("  Rejection comments: ");
                var rejectComments = Console.ReadLine()?.Trim() ?? "";

                proposal.Status = "Rejected";
                proposal.ReviewedBy = currentUser.Alias;
                proposal.ReviewedOn = DateTime.UtcNow;
                proposal.HRComments = rejectComments;
                await _promotionRepo.UpdateAsync(proposal);

                Console.WriteLine($"\n  Proposal for {proposal.EmployeeName} rejected.");
                return;
            }

            if (action != "approve")
            {
                Console.WriteLine("  Invalid action. Cancelled.");
                return;
            }

            // HR enters new level
            var allLevels = await _payrollRepo.GetAllLevelsAsync();
            if (allLevels.Count == 0)
            {
                Console.WriteLine("  No salary levels configured. Set up levels first.");
                return;
            }

            Console.WriteLine("\n  Available Levels:");
            for (int i = 0; i < allLevels.Count; i++)
            {
                var marker = allLevels[i].Level == proposal.CurrentLevel ? " ← current" : "";
                Console.WriteLine($"    {i + 1}. {allLevels[i].Level}: ${allLevels[i].MinSalary:N0} - ${allLevels[i].MaxSalary:N0}{marker}");
            }
            Console.Write("\n  Select new level: ");

            if (!int.TryParse(Console.ReadLine()?.Trim(), out int lvlSel) || lvlSel < 1 || lvlSel > allLevels.Count)
            {
                Console.WriteLine("  Invalid selection. Cancelled.");
                return;
            }

            var newLevel = allLevels[lvlSel - 1];

            // HR enters new salary
            Console.Write($"  New salary (${newLevel.MinSalary:N0} - ${newLevel.MaxSalary:N0}): $");
            if (!decimal.TryParse(Console.ReadLine()?.Trim(), out decimal newSalary))
            {
                Console.WriteLine("  Invalid amount. Cancelled.");
                return;
            }

            // Validate salary in range
            if (newSalary < newLevel.MinSalary || newSalary > newLevel.MaxSalary)
            {
                Console.WriteLine($"  Salary must be between ${newLevel.MinSalary:N0} and ${newLevel.MaxSalary:N0}. Cancelled.");
                return;
            }

            // Check budget
            var salaryIncrease = newSalary - proposal.CurrentSalary;
            if (employeeTeam != null && salaryIncrease > employeeTeam.Budget)
            {
                Console.WriteLine($"\n  ⚠ Budget insufficient! Increase: ${salaryIncrease:N0}, Available: ${employeeTeam.Budget:N0}");
                Console.Write("  Override and continue anyway? (yes/no): ");
                var overrideChoice = Console.ReadLine()?.Trim().ToLower();
                if (overrideChoice != "yes")
                {
                    Console.WriteLine("  Cancelled.");
                    return;
                }
            }

            // HR comments
            Console.Write("  Comments: ");
            var hrComments = Console.ReadLine()?.Trim() ?? "";

            // Confirm
            Console.WriteLine($"\n  --- Promotion Summary ---");
            Console.WriteLine($"  {proposal.EmployeeName}: {proposal.CurrentLevel} (${proposal.CurrentSalary:N0}) → {newLevel.Level} (${newSalary:N0})");
            Console.Write("  Confirm approval? (yes/no): ");
            var confirmApprove = Console.ReadLine()?.Trim().ToLower();
            if (confirmApprove != "yes")
            {
                Console.WriteLine("  Cancelled.");
                return;
            }

            // Process via Service Bus
            try
            {
                var promotionEvent = new
                {
                    proposal.EmployeeId,
                    proposal.Alias,
                    NewLevel = newLevel.Level,
                    NewSalary = newSalary,
                    OldSalary = proposal.CurrentSalary,
                    TeamId = employeeTeam?.TeamId ?? ""
                };

                // Update payroll via Service Bus
                await _serviceBus.PublishAndProcessAsync<bool>(
                    _queueNames["PromotionRaise"], promotionEvent, async json =>
                    {
                        var payroll = await _payrollRepo.GetPayrollByEmployeeIdAsync(proposal.EmployeeId);
                        if (payroll != null)
                        {
                            payroll.Level = newLevel.Level;
                            payroll.Salary = newSalary;
                            payroll.LastUpdated = DateTime.UtcNow;
                            await _payrollRepo.UpdatePayrollAsync(payroll);
                        }
                        return true;
                    });

                // Deduct team budget
                if (employeeTeam != null && salaryIncrease > 0)
                {
                    employeeTeam.Budget -= salaryIncrease;
                    await _teamRepo.UpdateTeamAsync(employeeTeam);
                }

                // Audit log
                var auditLog = new AuditLog
                {
                    Action = "Promotion Approved",
                    PerformedBy = currentUser.Alias,
                    PerformedByRole = currentUser.Role.ToString(),
                    TargetEmployeeId = proposal.EmployeeId,
                    Details = $"Promoted {proposal.EmployeeName} ({proposal.Alias}) from {proposal.CurrentLevel} (${proposal.CurrentSalary:N0}) to {newLevel.Level} (${newSalary:N0}). Proposed by {proposal.ProposedBy}."
                };
                await _auditRepo.LogAsync(auditLog);

                // Update promotion request
                proposal.Status = "Approved";
                proposal.NewLevel = newLevel.Level;
                proposal.NewSalary = newSalary;
                proposal.ReviewedBy = currentUser.Alias;
                proposal.ReviewedOn = DateTime.UtcNow;
                proposal.HRComments = hrComments;
                await _promotionRepo.UpdateAsync(proposal);

                Console.WriteLine($"\n  Promotion approved! Payroll updated for {proposal.EmployeeName}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n  Error processing promotion: {ex.Message}");
            }
        }
    }
}
