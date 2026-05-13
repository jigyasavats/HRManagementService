using HRManagementService.Models;
using HRManagementService.Repository;
using HRManagementService.Enums;

namespace HRManagementService.AuthService
{
    public class AuthManager
    {
        private readonly AuthRepository _authRepo;
        private readonly EmployeeRepository _employeeRepo;

        public AuthManager(AuthRepository authRepo, EmployeeRepository employeeRepo)
        {
            _authRepo = authRepo;
            _employeeRepo = employeeRepo;
        }

        public async Task<AuthUser?> LoginAsync()
        {
            const int maxAttempts = 3;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                Console.Write($"Enter your Alias (attempt {attempt}/{maxAttempts}): ");
                var alias = Console.ReadLine()?.Trim();

                if (string.IsNullOrEmpty(alias))
                {
                    Console.WriteLine("Alias cannot be empty.");
                    continue;
                }

                var user = await _authRepo.GetByAliasAsync(alias);
                if (user != null)
                {
                    if (!user.IsActive)
                    {
                        Console.WriteLine("\nAccount deactivated. Contact HR.");
                        continue;
                    }
                    Console.WriteLine($"\nWelcome, {user.Name}! Role: {user.Role}");
                    return user;
                }

                Console.WriteLine("\nUser not found.");
                var anyExists = await _authRepo.AnyUserExistsAsync();

                if (!anyExists)
                {
                    Console.Write("Are you setting up for the first time? (y/n): ");
                    var answer = Console.ReadLine()?.Trim().ToLower();
                    if (answer == "y")
                    {
                        return await FirstTimeSetupAsync(alias);
                    }
                }

                if (attempt < maxAttempts)
                    Console.WriteLine("Please try again.\n");
            }

            Console.WriteLine("Contact HR for access.");
            return null;
        }

        private async Task<AuthUser> FirstTimeSetupAsync(string alias)
        {
            Console.WriteLine("\n--- First Time Setup: HR Admin ---");

            Console.Write("Enter your name: ");
            var name = Console.ReadLine()?.Trim() ?? string.Empty;

            Console.Write("Enter your email: ");
            var email = Console.ReadLine()?.Trim() ?? string.Empty;

            var employeeId = Guid.NewGuid().ToString();

            // Create Employee record first
            var employee = new Employee
            {
                Id = employeeId,
                Name = name,
                Email = email,
                Alias = alias,
                JoiningDate = DateTime.UtcNow
            };
            await _employeeRepo.CreateEmployeeAsync(employee);

            // Create AuthUser with same employeeId
            var hrUser = new AuthUser
            {
                Id = Guid.NewGuid().ToString(),
                EmployeeId = employeeId,
                Name = name,
                Email = email,
                Alias = alias,
                Role = UserRole.HR
            };

            await _authRepo.CreateUserAsync(hrUser);
            Console.WriteLine($"\nHR Admin account created. Welcome, {name}!");
            return hrUser;
        }
    }
}
