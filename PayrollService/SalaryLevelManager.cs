using HRManagementService.Models;
using HRManagementService.Repository;
using HRManagementService.Enums;

namespace HRManagementService.PayrollService
{
    public class SalaryLevelManager
    {
        private readonly PayrollRepository _payrollRepo;
        private readonly EmployeeRepository _employeeRepo;
        private readonly TeamRepository _teamRepo;

        public SalaryLevelManager(PayrollRepository payrollRepo, EmployeeRepository employeeRepo, TeamRepository teamRepo)
        {
            _payrollRepo = payrollRepo;
            _employeeRepo = employeeRepo;
            _teamRepo = teamRepo;
        }

        public async Task SetupSalaryLevelsAsync()
        {
            Console.WriteLine("\n========================================");
            Console.WriteLine("   Setup Salary Levels");
            Console.WriteLine("========================================\n");

            var existing = await _payrollRepo.GetAllLevelsAsync();
            if (existing.Count > 0)
            {
                Console.WriteLine("Current Levels:");
                foreach (var lvl in existing)
                {
                    Console.WriteLine($"  {lvl.Level}: ${lvl.MinSalary:N0} - ${lvl.MaxSalary:N0}");
                }
                Console.WriteLine();
            }

            Console.WriteLine("Add a new salary level (or type 'done' to go back)\n");

            while (true)
            {
                Console.Write("Level name (e.g. L1, L2, Senior): ");
                var levelName = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(levelName) || levelName.ToLower() == "done")
                    break;

                var existingLevel = await _payrollRepo.GetLevelAsync(levelName);
                if (existingLevel != null)
                {
                    Console.WriteLine($"Level '{levelName}' already exists. Skipping.");
                    continue;
                }

                Console.Write("Min Salary: $");
                if (!decimal.TryParse(Console.ReadLine()?.Trim(), out decimal minSalary))
                {
                    Console.WriteLine("Invalid amount.");
                    continue;
                }

                Console.Write("Max Salary: $");
                if (!decimal.TryParse(Console.ReadLine()?.Trim(), out decimal maxSalary))
                {
                    Console.WriteLine("Invalid amount.");
                    continue;
                }

                if (maxSalary <= minSalary)
                {
                    Console.WriteLine("Max salary must be greater than min salary.");
                    continue;
                }

                var level = new LevelSalaryRange
                {
                    Id = Guid.NewGuid().ToString(),
                    Level = levelName,
                    MinSalary = minSalary,
                    MaxSalary = maxSalary
                };

                await _payrollRepo.CreateLevelAsync(level);
                Console.WriteLine($"Level '{levelName}' added! (${minSalary:N0} - ${maxSalary:N0})\n");
            }

            var all = await _payrollRepo.GetAllLevelsAsync();
            Console.WriteLine($"\n--- All Levels ({all.Count}) ---");
            foreach (var lvl in all)
            {
                Console.WriteLine($"  {lvl.Level}: ${lvl.MinSalary:N0} - ${lvl.MaxSalary:N0}");
            }
        }

        public async Task CheckSomeonesSalaryAsync(AuthUser currentUser, Func<Permission, string, Task<bool>>? scopeChecker = null)
        {
            Console.WriteLine("\n========================================");
            Console.WriteLine("   Check Employee Salary");
            Console.WriteLine("========================================\n");

            var searchable = await _employeeRepo.GetAllEmployeesAsync();
            searchable = searchable.Where(e => e.Alias != currentUser.Alias).ToList();

            if (searchable.Count == 0)
            {
                Console.WriteLine("No employees found.");
                return;
            }

            Console.WriteLine("  Select an employee:");
            for (int i = 0; i < searchable.Count; i++)
            {
                Console.WriteLine($"    {i + 1}. {searchable[i].Name} ({searchable[i].Alias})");
            }
            Console.Write("\n  Choice (0 to cancel): ");

            if (!int.TryParse(Console.ReadLine()?.Trim(), out int sel) || sel < 0 || sel > searchable.Count)
            {
                Console.WriteLine("  Invalid selection.");
                return;
            }
            if (sel == 0) return;

            var employee = searchable[sel - 1];

            if (scopeChecker != null && !await scopeChecker(Permission.CheckAnySalary, employee.Alias))
                return;

            var payroll = await _payrollRepo.GetPayrollByEmployeeIdAsync(employee.Id);

            if (payroll == null)
            {
                Console.WriteLine($"\n  No payroll record found for {employee.Name}.");
                return;
            }

            var levelInfo = await _payrollRepo.GetLevelAsync(payroll.Level);

            Console.WriteLine($"\n  --- Salary Details ---");
            Console.WriteLine($"  Employee:     {employee.Name} ({employee.Alias})");
            Console.WriteLine($"  Level:        {payroll.Level}");
            Console.WriteLine($"  Salary:       ${payroll.Salary:N0}");
            if (levelInfo != null)
                Console.WriteLine($"  Level Range:  ${levelInfo.MinSalary:N0} - ${levelInfo.MaxSalary:N0}");
            Console.WriteLine($"  Last Updated: {payroll.LastUpdated:yyyy-MM-dd}");
        }

        public async Task CheckOwnSalaryAsync(AuthUser currentUser)
        {
            Console.WriteLine("\n========================================");
            Console.WriteLine("   My Salary");
            Console.WriteLine("========================================\n");

            var employee = await _employeeRepo.GetByAliasAsync(currentUser.Alias);
            if (employee == null)
            {
                Console.WriteLine("Employee record not found.");
                return;
            }

            var payroll = await _payrollRepo.GetPayrollByEmployeeIdAsync(employee.Id);
            if (payroll == null)
            {
                Console.WriteLine("No payroll record found. Contact HR.");
                return;
            }

            var levelInfo = await _payrollRepo.GetLevelAsync(payroll.Level);

            Console.WriteLine($"  Name:         {employee.Name}");
            Console.WriteLine($"  Level:        {payroll.Level}");
            Console.WriteLine($"  Salary:       ${payroll.Salary:N0}");
            if (levelInfo != null)
                Console.WriteLine($"  Level Range:  ${levelInfo.MinSalary:N0} - ${levelInfo.MaxSalary:N0}");
            Console.WriteLine($"  Last Updated: {payroll.LastUpdated:yyyy-MM-dd}");
        }
    }
}
