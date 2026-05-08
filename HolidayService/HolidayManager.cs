using HRManagementService.Models;
using HRManagementService.Repository;

namespace HRManagementService.HolidayService
{
    public class HolidayManager
    {
        private readonly HolidayRepository _holidayRepo;
        private readonly EmployeeRepository _employeeRepo;
        private readonly TeamRepository _teamRepo;

        public HolidayManager(HolidayRepository holidayRepo, EmployeeRepository employeeRepo, TeamRepository teamRepo)
        {
            _holidayRepo = holidayRepo;
            _employeeRepo = employeeRepo;
            _teamRepo = teamRepo;
        }

        public async Task SetupHolidayConfigAsync()
        {
            Console.WriteLine("\n========================================");
            Console.WriteLine("   Setup Holiday Config");
            Console.WriteLine("========================================\n");

            var config = await _holidayRepo.GetConfigAsync();
            if (config == null)
            {
                config = new HolidayConfig();
            }

            // Show current config
            Console.WriteLine($"Current Annual Leave Count: {config.AnnualLeaveCount}");
            Console.WriteLine($"Fixed Holidays: {config.FixedHolidays.Count}");
            if (config.FixedHolidays.Count > 0)
            {
                foreach (var h in config.FixedHolidays.OrderBy(h => h.Date))
                {
                    Console.WriteLine($"  - {h.Name} ({h.Date:yyyy-MM-dd})");
                }
            }

            Console.WriteLine("\nWhat do you want to do?");
            Console.WriteLine("  1. Set Annual Leave Count");
            Console.WriteLine("  2. Add Fixed Holiday");
            Console.WriteLine("  3. Remove Fixed Holiday");
            Console.WriteLine("  4. View Current Config");
            Console.WriteLine("  5. Back");
            Console.Write("\nChoice: ");
            var choice = Console.ReadLine()?.Trim();

            switch (choice)
            {
                case "1":
                    Console.Write($"New Annual Leave Count (current: {config.AnnualLeaveCount}): ");
                    if (!int.TryParse(Console.ReadLine()?.Trim(), out int leaveCount) || leaveCount < 0)
                    {
                        Console.WriteLine("Invalid number.");
                        return;
                    }
                    config.AnnualLeaveCount = leaveCount;
                    await _holidayRepo.UpsertConfigAsync(config);
                    Console.WriteLine($"Annual leave count set to {leaveCount}.");
                    break;

                case "2":
                    Console.Write("Holiday Name (e.g. Diwali): ");
                    var holidayName = Console.ReadLine()?.Trim();
                    if (string.IsNullOrEmpty(holidayName))
                    {
                        Console.WriteLine("Name is required.");
                        return;
                    }

                    Console.Write("Date (yyyy-MM-dd): ");
                    if (!DateTime.TryParse(Console.ReadLine()?.Trim(), out DateTime holidayDate))
                    {
                        Console.WriteLine("Invalid date format.");
                        return;
                    }

                    var duplicate = config.FixedHolidays.Any(h =>
                        h.Name.Equals(holidayName, StringComparison.OrdinalIgnoreCase));
                    if (duplicate)
                    {
                        Console.WriteLine($"'{holidayName}' already exists.");
                        return;
                    }

                    config.FixedHolidays.Add(new FixedHoliday
                    {
                        Name = holidayName,
                        Date = holidayDate
                    });
                    await _holidayRepo.UpsertConfigAsync(config);
                    Console.WriteLine($"Added '{holidayName}' on {holidayDate:yyyy-MM-dd}.");
                    break;

                case "3":
                    if (config.FixedHolidays.Count == 0)
                    {
                        Console.WriteLine("No fixed holidays to remove.");
                        return;
                    }

                    for (int i = 0; i < config.FixedHolidays.Count; i++)
                    {
                        Console.WriteLine($"  {i + 1}. {config.FixedHolidays[i].Name} ({config.FixedHolidays[i].Date:yyyy-MM-dd})");
                    }
                    Console.Write("Select holiday to remove: ");
                    if (!int.TryParse(Console.ReadLine()?.Trim(), out int removeIndex) ||
                        removeIndex < 1 || removeIndex > config.FixedHolidays.Count)
                    {
                        Console.WriteLine("Invalid selection.");
                        return;
                    }

                    var removed = config.FixedHolidays[removeIndex - 1];
                    config.FixedHolidays.RemoveAt(removeIndex - 1);
                    await _holidayRepo.UpsertConfigAsync(config);
                    Console.WriteLine($"Removed '{removed.Name}'.");
                    break;

                case "4":
                    Console.WriteLine($"\n--- Holiday Config ---");
                    Console.WriteLine($"Annual Leave: {config.AnnualLeaveCount} days");
                    Console.WriteLine($"\nFixed Holidays ({config.FixedHolidays.Count}):");
                    if (config.FixedHolidays.Count == 0)
                        Console.WriteLine("  (none)");
                    else
                        foreach (var h in config.FixedHolidays.OrderBy(h => h.Date))
                            Console.WriteLine($"  - {h.Name} ({h.Date:yyyy-MM-dd})");
                    break;

                default:
                    Console.WriteLine("Going back.");
                    break;
            }
        }

        public async Task CheckHolidaysAsync()
        {
            Console.WriteLine("\n========================================");
            Console.WriteLine("   Fixed Holidays");
            Console.WriteLine("========================================\n");

            var config = await _holidayRepo.GetConfigAsync();
            if (config == null || config.FixedHolidays.Count == 0)
            {
                Console.WriteLine("No fixed holidays configured.");
                return;
            }

            Console.WriteLine($"Annual Leave: {config.AnnualLeaveCount} days\n");
            Console.WriteLine($"Fixed Holidays ({config.FixedHolidays.Count}):");
            foreach (var h in config.FixedHolidays.OrderBy(h => h.Date))
            {
                var isPast = h.Date.Date < DateTime.UtcNow.Date;
                var tag = isPast ? " (past)" : "";
                Console.WriteLine($"  - {h.Date:yyyy-MM-dd}  {h.Name}{tag}");
            }
        }

        public async Task RequestHolidayAsync(AuthUser currentUser)
        {
            Console.WriteLine("\n========================================");
            Console.WriteLine("   Request Holiday");
            Console.WriteLine("========================================\n");

            var employee = await _employeeRepo.GetByIdAsync(currentUser.EmployeeId);
            if (employee == null)
            {
                Console.WriteLine("Employee record not found.");
                return;
            }

            var bank = await _holidayRepo.GetHolidayBankByEmployeeIdAsync(currentUser.EmployeeId);
            if (bank == null)
            {
                Console.WriteLine("Holiday bank not found. Contact HR.");
                return;
            }

            var pendingCount = bank.Requests.Count(r => r.Status == "Pending");
            var pendingDays = bank.Requests
                .Where(r => r.Status == "Pending")
                .Sum(r => (r.EndDate - r.StartDate).Days + 1);

            Console.WriteLine($"  Employee: {employee.Name}");
            Console.WriteLine($"  Annual Leave Balance: {bank.AnnualLeaveBalance} days");
            Console.WriteLine($"  Pending Requests: {pendingCount} ({pendingDays} days)");
            Console.WriteLine($"  Available: {bank.AnnualLeaveBalance - pendingDays} days\n");

            Console.Write("Start Date (yyyy-MM-dd): ");
            if (!DateTime.TryParse(Console.ReadLine()?.Trim(), out DateTime startDate))
            {
                Console.WriteLine("Invalid date.");
                return;
            }

            Console.Write("End Date (yyyy-MM-dd): ");
            if (!DateTime.TryParse(Console.ReadLine()?.Trim(), out DateTime endDate))
            {
                Console.WriteLine("Invalid date.");
                return;
            }

            if (endDate < startDate)
            {
                Console.WriteLine("End date must be after start date.");
                return;
            }

            var requestedDays = (endDate - startDate).Days + 1;
            var remaining = bank.AnnualLeaveBalance - pendingDays;

            if (requestedDays > remaining)
            {
                Console.WriteLine($"Not enough leave balance. Requesting {requestedDays} days but only {remaining} remaining.");
                return;
            }

            Console.Write("Reason: ");
            var reason = Console.ReadLine()?.Trim() ?? string.Empty;

            Console.WriteLine($"\n--- Confirm ---");
            Console.WriteLine($"  From: {startDate:yyyy-MM-dd}");
            Console.WriteLine($"  To:   {endDate:yyyy-MM-dd}");
            Console.WriteLine($"  Days: {requestedDays}");
            Console.WriteLine($"  Reason: {reason}");
            Console.Write("\nSubmit request? (y/n): ");

            if (Console.ReadLine()?.Trim().ToLower() != "y")
            {
                Console.WriteLine("Cancelled.");
                return;
            }

            bank.Requests.Add(new HolidayRequest
            {
                StartDate = startDate,
                EndDate = endDate,
                Reason = reason,
                EmployeeName = employee.Name
            });

            await _holidayRepo.UpdateHolidayBankAsync(bank);
            Console.WriteLine("\nHoliday request submitted! Waiting for manager approval.");
        }

        public async Task ApproveRejectHolidayAsync(AuthUser currentUser)
        {
            Console.WriteLine("\n========================================");
            Console.WriteLine("   Approve / Reject Holiday Requests");
            Console.WriteLine("========================================\n");

            // Find teams where current user is the manager (by alias)
            var currentEmployee = await _employeeRepo.GetByIdAsync(currentUser.EmployeeId);
            if (currentEmployee == null)
            {
                Console.WriteLine("Employee record not found.");
                return;
            }

            var allTeams = await _teamRepo.GetAllTeamsAsync();
            var myTeams = allTeams.Where(t => t.ManagerId == currentEmployee.Alias).ToList();

            if (myTeams.Count == 0)
            {
                Console.WriteLine("You are not a manager of any team.");
                return;
            }

            // Get team member aliases → resolve to employee GUIDs
            var teamMemberAliases = myTeams.SelectMany(t => t.EmployeeIds).Distinct().ToList();
            if (teamMemberAliases.Count == 0)
            {
                Console.WriteLine("No team members found.");
                return;
            }

            // Collect all pending requests from team members
            var pendingItems = new List<(EmployeeHolidayBank bank, HolidayRequest request)>();
            foreach (var alias in teamMemberAliases)
            {
                var emp = await _employeeRepo.GetByAliasAsync(alias);
                if (emp == null) continue;

                var bank = await _holidayRepo.GetHolidayBankByEmployeeIdAsync(emp.Id);
                if (bank == null) continue;

                foreach (var req in bank.Requests.Where(r => r.Status == "Pending"))
                {
                    pendingItems.Add((bank, req));
                }
            }

            if (pendingItems.Count == 0)
            {
                Console.WriteLine("No pending holiday requests from your team.");
                return;
            }

            Console.WriteLine($"Pending Requests ({pendingItems.Count}):\n");
            for (int i = 0; i < pendingItems.Count; i++)
            {
                var (bank, req) = pendingItems[i];
                var days = (req.EndDate - req.StartDate).Days + 1;
                Console.WriteLine($"  {i + 1}. {req.EmployeeName} (ID: {bank.EmployeeId})");
                Console.WriteLine($"     {req.StartDate:yyyy-MM-dd} to {req.EndDate:yyyy-MM-dd} ({days} days)");
                Console.WriteLine($"     Reason: {req.Reason}");
                Console.WriteLine($"     Requested: {req.RequestedOn:yyyy-MM-dd HH:mm}\n");
            }

            Console.Write("Select request number (or 0 to go back): ");
            if (!int.TryParse(Console.ReadLine()?.Trim(), out int selection) || selection < 0 || selection > pendingItems.Count)
            {
                Console.WriteLine("Invalid selection.");
                return;
            }
            if (selection == 0) return;

            var selected = pendingItems[selection - 1];

            Console.WriteLine("  1. Approve");
            Console.WriteLine("  2. Reject");
            Console.Write("Choice: ");
            var action = Console.ReadLine()?.Trim();

            if (action == "1")
            {
                selected.request.Status = "Approved";
                // Deduct leave days from balance
                int leaveDays = (int)(selected.request.EndDate - selected.request.StartDate).TotalDays + 1;
                selected.bank.AnnualLeaveBalance -= leaveDays;
                await _holidayRepo.UpdateHolidayBankAsync(selected.bank);
                Console.WriteLine($"\nApproved! {selected.request.EmployeeName}'s leave from {selected.request.StartDate:yyyy-MM-dd} to {selected.request.EndDate:yyyy-MM-dd}.");
                Console.WriteLine($"  Days deducted: {leaveDays} | Remaining balance: {selected.bank.AnnualLeaveBalance}");
            }
            else if (action == "2")
            {
                selected.request.Status = "Rejected";
                await _holidayRepo.UpdateHolidayBankAsync(selected.bank);
                Console.WriteLine($"\nRejected. {selected.request.EmployeeName}'s leave request has been declined.");
            }
            else
            {
                Console.WriteLine("Invalid choice.");
            }
        }

        public async Task CheckOwnHolidayBankAsync(AuthUser currentUser)
        {
            Console.WriteLine("\n========================================");
            Console.WriteLine("   My Holiday Bank");
            Console.WriteLine("========================================\n");

            var employee = await _employeeRepo.GetByIdAsync(currentUser.EmployeeId);
            if (employee == null)
            {
                Console.WriteLine("Employee record not found.");
                return;
            }

            var bank = await _holidayRepo.GetHolidayBankByEmployeeIdAsync(currentUser.EmployeeId);
            if (bank == null)
            {
                Console.WriteLine("Holiday bank not found. Contact HR.");
                return;
            }

            var approvedDays = bank.Requests
                .Where(r => r.Status == "Approved")
                .Sum(r => (r.EndDate - r.StartDate).Days + 1);
            var pendingDays = bank.Requests
                .Where(r => r.Status == "Pending")
                .Sum(r => (r.EndDate - r.StartDate).Days + 1);

            Console.WriteLine($"  Employee:       {employee.Name}");
            Console.WriteLine($"  Annual Balance: {bank.AnnualLeaveBalance} days");
            Console.WriteLine($"  Pending:        {pendingDays} days");
            Console.WriteLine($"  Available:      {bank.AnnualLeaveBalance - pendingDays} days");

            if (bank.Requests.Count == 0)
            {
                Console.WriteLine("\n  No holiday requests yet.");
                return;
            }

            Console.WriteLine($"\n--- Request History ({bank.Requests.Count}) ---\n");
            foreach (var req in bank.Requests.OrderByDescending(r => r.RequestedOn))
            {
                var days = (req.EndDate - req.StartDate).Days + 1;
                var statusIcon = req.Status switch
                {
                    "Approved" => "[APPROVED]",
                    "Rejected" => "[REJECTED]",
                    "Pending" => "[PENDING]",
                    _ => "[UNKNOWN]"
                };
                Console.WriteLine($"  {req.StartDate:yyyy-MM-dd} to {req.EndDate:yyyy-MM-dd} ({days} days) — {statusIcon}");
                Console.WriteLine($"    Reason: {req.Reason}");
                Console.WriteLine($"    Requested: {req.RequestedOn:yyyy-MM-dd HH:mm}\n");
            }
        }
    }
}
