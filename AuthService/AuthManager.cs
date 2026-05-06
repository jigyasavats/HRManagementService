using HRManagementService.Models;
using HRManagementService.Repository;
using HRManagementService.Enums;

namespace HRManagementService.AuthService
{
    public class AuthManager
    {
        private readonly AuthRepository _authRepo;

        public AuthManager(AuthRepository authRepo)
        {
            _authRepo = authRepo;
        }

        public async Task<AuthUser?> LoginAsync()
        {
            Console.Write("Enter your Employee ID: ");
            var employeeId = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(employeeId))
            {
                Console.WriteLine("Employee ID cannot be empty.");
                return null;
            }

            var user = await _authRepo.GetByEmployeeIdAsync(employeeId);
            if (user != null)
            {
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
                    return await FirstTimeSetupAsync(employeeId);
                }
            }

            Console.WriteLine("Contact HR for access.");
            return null;
        }

        private async Task<AuthUser> FirstTimeSetupAsync(string employeeId)
        {
            Console.WriteLine("\n--- First Time Setup: HR Admin ---");

            Console.Write("Enter your name: ");
            var name = Console.ReadLine()?.Trim() ?? string.Empty;

            Console.Write("Enter your email: ");
            var email = Console.ReadLine()?.Trim() ?? string.Empty;

            var hrUser = new AuthUser
            {
                Id = employeeId,
                EmployeeId = employeeId,
                Name = name,
                Email = email,
                Role = UserRole.HR
            };

            await _authRepo.CreateUserAsync(hrUser);
            Console.WriteLine($"\nHR Admin account created. Welcome, {name}!");
            return hrUser;
        }
    }
}
