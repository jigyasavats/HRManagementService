using HRManagementService.Models;
using HRManagementService.Repository;

namespace HRManagementService.TeamService
{
    public class TeamManager
    {
        private readonly TeamRepository _teamRepo;
        private readonly AuthRepository _authRepo;

        public TeamManager(TeamRepository teamRepo, AuthRepository authRepo)
        {
            _teamRepo = teamRepo;
            _authRepo = authRepo;
        }

        private async Task<string> SelectManagerAsync(string label, string currentValue)
        {
            var allUsers = await _authRepo.GetAllUsersAsync();
            // Show managers and employees (exclude HR)
            var candidates = allUsers.Where(u => u.Role != Enums.UserRole.HR).ToList();

            if (candidates.Count == 0)
            {
                Console.WriteLine("  No employees found in the system.");
                Console.Write($"  Enter {label} alias manually (or press Enter to skip): ");
                return Console.ReadLine()?.Trim() ?? string.Empty;
            }

            Console.WriteLine($"\n  Available for {label}:");
            for (int i = 0; i < candidates.Count; i++)
            {
                var current = candidates[i].Alias == currentValue ? " (current)" : "";
                var role = candidates[i].Role == Enums.UserRole.Manager ? "Manager" : "Employee";
                Console.WriteLine($"    {i + 1}. {candidates[i].Name} ({candidates[i].Alias}) [{role}]{current}");
            }
            Console.WriteLine($"    {candidates.Count + 1}. None / Clear");
            Console.Write($"\n  Select {label}: ");

            if (!int.TryParse(Console.ReadLine()?.Trim(), out int sel) || sel < 1 || sel > candidates.Count + 1)
            {
                Console.WriteLine("  Invalid selection. Keeping current value.");
                return currentValue;
            }

            if (sel == candidates.Count + 1)
                return string.Empty;

            var selected = candidates[sel - 1];

            // If selecting an Employee as manager, promote their role
            if (selected.Role == Enums.UserRole.Employee)
            {
                selected.Role = Enums.UserRole.Manager;
                await _authRepo.UpdateUserAsync(selected);
                Console.WriteLine($"  {selected.Name} promoted to Manager role.");
            }

            return selected.Alias;
        }

        public async Task CreateTeamAsync()
        {
            Console.WriteLine("\n========================================");
            Console.WriteLine("   Create Team");
            Console.WriteLine("========================================\n");

            Console.Write("Team Name: ");
            var teamName = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(teamName))
            {
                Console.WriteLine("Team name is required.");
                return;
            }

            Console.Write("Team ID (short unique code, e.g. 'eng-platform'): ");
            var teamId = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(teamId))
            {
                Console.WriteLine("Team ID is required.");
                return;
            }

            var existing = await _teamRepo.GetByTeamIdAsync(teamId);
            if (existing != null)
            {
                Console.WriteLine($"Team with ID '{teamId}' already exists.");
                return;
            }

            var managerId = await SelectManagerAsync("Manager", "");
            var skipManagerId = await SelectManagerAsync("Skip Manager", "");

            Console.Write("Team Budget: $");
            if (!decimal.TryParse(Console.ReadLine()?.Trim(), out decimal budget))
            {
                Console.WriteLine("Invalid budget.");
                return;
            }

            var team = new Team
            {
                Id = Guid.NewGuid().ToString(),
                TeamId = teamId,
                TeamName = teamName,
                ManagerId = managerId,
                SkipManagerId = skipManagerId,
                Budget = budget
            };

            Console.WriteLine("\n--- Confirm ---");
            Console.WriteLine($"  Name:         {team.TeamName}");
            Console.WriteLine($"  Team ID:      {team.TeamId}");
            Console.WriteLine($"  Manager:      {(string.IsNullOrEmpty(managerId) ? "(none)" : managerId)}");
            Console.WriteLine($"  Skip Manager: {(string.IsNullOrEmpty(skipManagerId) ? "(none)" : skipManagerId)}");
            Console.WriteLine($"  Budget:       ${team.Budget:N0}");
            Console.Write("\nProceed? (y/n): ");

            if (Console.ReadLine()?.Trim().ToLower() != "y")
            {
                Console.WriteLine("Cancelled.");
                return;
            }

            await _teamRepo.CreateTeamAsync(team);
            Console.WriteLine($"\nTeam '{teamName}' created successfully!");
        }

        public async Task UpdateTeamAsync()
        {
            var teams = await _teamRepo.GetAllTeamsAsync();
            if (teams.Count == 0)
            {
                Console.WriteLine("\nNo teams found.");
                return;
            }

            Console.WriteLine("\n========================================");
            Console.WriteLine("   Update Team");
            Console.WriteLine("========================================\n");

            for (int i = 0; i < teams.Count; i++)
            {
                Console.WriteLine($"  {i + 1}. {teams[i].TeamName} ({teams[i].TeamId})");
            }
            Console.Write("\nSelect team number: ");
            if (!int.TryParse(Console.ReadLine()?.Trim(), out int teamIndex) || teamIndex < 1 || teamIndex > teams.Count)
            {
                Console.WriteLine("Invalid selection.");
                return;
            }

            var team = teams[teamIndex - 1];

            Console.WriteLine($"\nSelected: {team.TeamName} ({team.TeamId})");
            Console.WriteLine($"  Manager:      {(string.IsNullOrEmpty(team.ManagerId) ? "(none)" : team.ManagerId)}");
            Console.WriteLine($"  Skip Manager: {(string.IsNullOrEmpty(team.SkipManagerId) ? "(none)" : team.SkipManagerId)}");
            Console.WriteLine($"  Budget:       ${team.Budget:N0}");
            Console.WriteLine($"  Members:      {team.EmployeeIds.Count}");

            Console.WriteLine("\nWhat do you want to update?");
            Console.WriteLine("  1. Manager");
            Console.WriteLine("  2. Skip Manager");
            Console.WriteLine("  3. Budget");
            Console.WriteLine("  4. Team Name");
            Console.WriteLine("  5. Add Member (unassigned)");
            Console.WriteLine("  6. Transfer Member (from another team)");
            Console.WriteLine("  7. Remove Member");
            Console.WriteLine("  8. Cancel");
            Console.Write("\nChoice: ");
            var choice = Console.ReadLine()?.Trim();

            switch (choice)
            {
                case "1":
                    var newManager = await SelectManagerAsync("Manager", team.ManagerId);
                    team.ManagerId = newManager;
                    await _teamRepo.UpdateTeamAsync(team);
                    Console.WriteLine("Manager updated.");
                    break;

                case "2":
                    var newSkip = await SelectManagerAsync("Skip Manager", team.SkipManagerId);
                    team.SkipManagerId = newSkip;
                    await _teamRepo.UpdateTeamAsync(team);
                    Console.WriteLine("Skip Manager updated.");
                    break;

                case "3":
                    Console.Write($"New Budget (current: ${team.Budget:N0}): $");
                    if (!decimal.TryParse(Console.ReadLine()?.Trim(), out decimal newBudget))
                    {
                        Console.WriteLine("Invalid amount.");
                        return;
                    }
                    team.Budget = newBudget;
                    await _teamRepo.UpdateTeamAsync(team);
                    Console.WriteLine("Budget updated.");
                    break;

                case "4":
                    Console.Write($"New Team Name (current: {team.TeamName}): ");
                    var newName = Console.ReadLine()?.Trim();
                    if (string.IsNullOrEmpty(newName))
                    {
                        Console.WriteLine("Name is required.");
                        return;
                    }
                    team.TeamName = newName;
                    await _teamRepo.UpdateTeamAsync(team);
                    Console.WriteLine("Team name updated.");
                    break;

                case "5":
                    await AddMemberToTeamAsync(team);
                    break;

                case "6":
                    await TransferMemberToTeamAsync(team);
                    break;

                case "7":
                    await RemoveMemberFromTeamAsync(team);
                    break;

                default:
                    Console.WriteLine("Cancelled.");
                    break;
            }
        }

        private async Task AddMemberToTeamAsync(Team team)
        {
            var allUsers = await _authRepo.GetAllUsersAsync();
            var allTeams = await _teamRepo.GetAllTeamsAsync();

            // Collect aliases already in a team's employeeIds (managers/skip managers can still be added as members)
            var assignedAliases = new HashSet<string>();
            foreach (var t in allTeams)
            {
                foreach (var alias in t.EmployeeIds) assignedAliases.Add(alias);
            }

            // Show employees not already in any team's employeeIds, and not this team's own manager/skip
            var available = allUsers.Where(u =>
                u.Role != Enums.UserRole.HR &&
                !assignedAliases.Contains(u.Alias) &&
                u.Alias != team.ManagerId &&
                u.Alias != team.SkipManagerId).ToList();

            if (available.Count == 0)
            {
                Console.WriteLine("  No available employees to add.");
                return;
            }

            Console.WriteLine("\n  Available Employees (not assigned to any team):");
            for (int i = 0; i < available.Count; i++)
            {
                var role = available[i].Role == Enums.UserRole.Manager ? "Manager" : "Employee";
                Console.WriteLine($"    {i + 1}. {available[i].Name} ({available[i].Alias}) [{role}]");
            }
            Console.Write("\n  Select employee to add (0 to cancel): ");

            if (!int.TryParse(Console.ReadLine()?.Trim(), out int sel) || sel < 0 || sel > available.Count)
            {
                Console.WriteLine("  Invalid selection.");
                return;
            }
            if (sel == 0) return;

            var selected = available[sel - 1];
            team.EmployeeIds.Add(selected.Alias);
            await _teamRepo.UpdateTeamAsync(team);
            Console.WriteLine($"  {selected.Name} added to {team.TeamName}.");
        }

        private async Task TransferMemberToTeamAsync(Team team)
        {
            var allUsers = await _authRepo.GetAllUsersAsync();
            var userLookup = allUsers.ToDictionary(u => u.Alias, u => u);
            var allTeams = await _teamRepo.GetAllTeamsAsync();

            // Collect employees in other teams (not managers/skip — only employeeIds)
            var transferable = new List<(string Alias, Team FromTeam)>();
            foreach (var t in allTeams)
            {
                if (t.TeamId == team.TeamId) continue;
                foreach (var alias in t.EmployeeIds)
                    transferable.Add((alias, t));
            }

            if (transferable.Count == 0)
            {
                Console.WriteLine("  No employees in other teams to transfer.");
                return;
            }

            Console.WriteLine($"\n  Employees in other teams (transfer to '{team.TeamName}'):");
            for (int i = 0; i < transferable.Count; i++)
            {
                var (alias, fromTeam) = transferable[i];
                var role = "Employee";
                var name = alias;
                if (userLookup.TryGetValue(alias, out var user))
                {
                    name = user.Name;
                    role = user.Role == Enums.UserRole.Manager ? "Manager" : "Employee";
                }
                Console.WriteLine($"    {i + 1}. {name} ({alias}) [{role}] — currently in '{fromTeam.TeamName}'");
            }
            Console.Write("\n  Select employee to transfer (0 to cancel): ");

            if (!int.TryParse(Console.ReadLine()?.Trim(), out int sel) || sel < 0 || sel > transferable.Count)
            {
                Console.WriteLine("  Invalid selection.");
                return;
            }
            if (sel == 0) return;

            var (selectedAlias, sourceTeam) = transferable[sel - 1];
            var selectedName = userLookup.TryGetValue(selectedAlias, out var u2) ? u2.Name : selectedAlias;

            Console.Write($"  Transfer {selectedName} from '{sourceTeam.TeamName}' to '{team.TeamName}'? (y/n): ");
            if (Console.ReadLine()?.Trim().ToLower() != "y")
            {
                Console.WriteLine("  Cancelled.");
                return;
            }

            // Remove from old team
            sourceTeam.EmployeeIds.Remove(selectedAlias);
            await _teamRepo.UpdateTeamAsync(sourceTeam);

            // Add to new team
            team.EmployeeIds.Add(selectedAlias);
            await _teamRepo.UpdateTeamAsync(team);

            Console.WriteLine($"  {selectedName} transferred from '{sourceTeam.TeamName}' to '{team.TeamName}'.");
        }

        private async Task RemoveMemberFromTeamAsync(Team team)
        {
            if (team.EmployeeIds.Count == 0)
            {
                Console.WriteLine("  No members in this team.");
                return;
            }

            var allUsers = await _authRepo.GetAllUsersAsync();
            var userLookup = allUsers.ToDictionary(u => u.Alias, u => u);

            Console.WriteLine($"\n  Current Members of '{team.TeamName}':");
            for (int i = 0; i < team.EmployeeIds.Count; i++)
            {
                var alias = team.EmployeeIds[i];
                if (userLookup.TryGetValue(alias, out var user))
                {
                    var role = user.Role == Enums.UserRole.Manager ? "Manager" : "Employee";
                    Console.WriteLine($"    {i + 1}. {user.Name} ({alias}) [{role}]");
                }
                else
                {
                    Console.WriteLine($"    {i + 1}. {alias} [unknown]");
                }
            }
            Console.Write("\n  Select member to remove (0 to cancel): ");

            if (!int.TryParse(Console.ReadLine()?.Trim(), out int sel) || sel < 0 || sel > team.EmployeeIds.Count)
            {
                Console.WriteLine("  Invalid selection.");
                return;
            }
            if (sel == 0) return;

            var removed = team.EmployeeIds[sel - 1];
            team.EmployeeIds.RemoveAt(sel - 1);
            await _teamRepo.UpdateTeamAsync(team);
            Console.WriteLine($"  {removed} removed from {team.TeamName}.");
        }

        public async Task ViewAllTeamsAsync()
        {
            var teams = await _teamRepo.GetAllTeamsAsync();
            if (teams.Count == 0)
            {
                Console.WriteLine("\nNo teams found.");
                return;
            }

            Console.WriteLine($"\n--- All Teams ({teams.Count}) ---");
            foreach (var team in teams)
            {
                Console.WriteLine($"  {team.TeamName} ({team.TeamId}) | Members: {team.EmployeeIds.Count} | Budget: ${team.Budget:N0}");
            }
        }
    }
}
