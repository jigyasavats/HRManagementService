using HRManagementService.Enums;
using HRManagementService.Models;
using HRManagementService.AuthService.Rules;

namespace HRManagementService;

public class MenuRouter
{
    private readonly Bootstrap.AppServices _app;

    public MenuRouter(Bootstrap.AppServices app)
    {
        _app = app;
    }

    public void ShowMenu(UserRole role)
    {
        Console.WriteLine("\n========================================");
        Console.WriteLine($"  Menu ({role})");
        Console.WriteLine("========================================");

        if (role == UserRole.HR)
        {
            Console.WriteLine("  1.  Employee Actions");
            Console.WriteLine("  2.  Setup Salary Levels");
            Console.WriteLine("  3.  Holidays");
            Console.WriteLine("  4.  Team Management");
            Console.WriteLine("  5.  Payroll");
            Console.WriteLine("  6.  Update Personal Info");
            Console.WriteLine("  7.  Ask HR Bot");
            Console.WriteLine("  8.  Active Sessions");
            Console.WriteLine("  9.  Logout");
            Console.WriteLine("  10. Exit");
        }
        else if (role == UserRole.Manager)
        {
            Console.WriteLine("  1.  Performance Reviews");
            Console.WriteLine("  2.  Propose Promotion");
            Console.WriteLine("  3.  Payroll");
            Console.WriteLine("  4.  Holidays");
            Console.WriteLine("  5.  Update Personal Info");
            Console.WriteLine("  6.  Ask HR Bot");
            Console.WriteLine("  7.  Logout");
            Console.WriteLine("  8.  Exit");
        }
        else
        {
            Console.WriteLine("  1. Holidays");
            Console.WriteLine("  2. Check Own Salary");
            Console.WriteLine("  3. Update Personal Info");
            Console.WriteLine("  4. Submit Own Performance Review");
            Console.WriteLine("  5. Ask HR Bot");
            Console.WriteLine("  6. Logout");
            Console.WriteLine("  7. Exit");
        }
    }

    public string GetLogoutOption(UserRole role) => role switch
    {
        UserRole.HR => "9",
        UserRole.Manager => "7",
        UserRole.Employee => "6",
        _ => "1"
    };

    public string GetExitOption(UserRole role) => role switch
    {
        UserRole.HR => "10",
        UserRole.Manager => "8",
        UserRole.Employee => "7",
        _ => "1"
    };

    public async Task HandleActionAsync(string input, AuthUser currentUser, Func<Permission, string, Task<bool>> scopeChecker)
    {
        var role = currentUser.Role;

        // Top-level permission check
        var topPermission = _app.AuthzService.GetRequiredPermission(role, input);
        if (topPermission != null && !await CheckAccessAsync(currentUser, topPermission.Value))
            return;

        if (role == UserRole.HR && input == "1")
            await HandleHREmployeeActionsAsync(currentUser, scopeChecker);
        else if (role == UserRole.HR && input == "2")
            await _app.SalaryLevelManager.SetupSalaryLevelsAsync();
        else if (IsHolidayMenu(role, input))
            await HandleHolidaysAsync(currentUser, input);
        else if (role == UserRole.HR && input == "4")
            await HandleTeamManagementAsync(currentUser);
        else if (role == UserRole.HR && input == "5")
            await _app.SalaryLevelManager.CheckSomeonesSalaryAsync(currentUser, scopeChecker);
        else if (role == UserRole.Manager && input == "1")
            await HandleManagerPerformanceAsync(currentUser, scopeChecker);
        else if (role == UserRole.Manager && input == "2")
            await _app.EmployeeManager.ProposePromotionAsync(currentUser, scopeChecker);
        else if (role == UserRole.Manager && input == "3")
            await HandleManagerPayrollAsync(currentUser, scopeChecker);
        else if (role == UserRole.Employee && input == "2")
            await _app.SalaryLevelManager.CheckOwnSalaryAsync(currentUser);
        else if (role == UserRole.Employee && input == "4")
            await HandleEmployeePerformanceAsync(currentUser);
        else if (IsUpdateInfoMenu(role, input))
            await _app.EmployeeManager.UpdatePersonalInfoAsync(currentUser);
        else if (IsHRBotMenu(role, input))
            await _app.AIManager.StartHRChatbotAsync();
        else if (role == UserRole.HR && input == "8")
            await HandleActiveSessionsAsync(currentUser);
        else
            Console.WriteLine("This feature is coming soon!");
    }

    private async Task HandleHREmployeeActionsAsync(AuthUser currentUser, Func<Permission, string, Task<bool>> scopeChecker)
    {
        Console.WriteLine("\n  1. Add New Employee");
        Console.WriteLine("  2. Terminate Employee");
        Console.WriteLine("  3. Give Promotion");
        Console.WriteLine("  4. Check Pipeline Status");
        Console.Write("\nChoice: ");
        var subChoice = Console.ReadLine()?.Trim();

        if (!await CheckSubPermissionAsync(currentUser, "1", subChoice))
            return;

        if (subChoice == "1")
            await _app.EmployeeManager.AddNewEmployeeAsync(currentUser);
        else if (subChoice == "2")
            await _app.EmployeeManager.TerminateEmployeeAsync(currentUser, scopeChecker);
        else if (subChoice == "3")
            await _app.EmployeeManager.ReviewPromotionAsync(currentUser);
        else if (subChoice == "4")
            await _app.EmployeeManager.CheckOnboardingStatusAsync();
        else
            Console.WriteLine("Invalid choice.");
    }

    private async Task HandleHolidaysAsync(AuthUser currentUser, string input)
    {
        Console.WriteLine("\n  1. View Fixed Holidays");
        Console.WriteLine("  2. Request Holiday");
        Console.WriteLine("  3. Check Own Holiday Bank");
        if (currentUser.Role == UserRole.HR)
            Console.WriteLine("  4. Setup Holiday Config");
        if (currentUser.Role == UserRole.Manager)
            Console.WriteLine("  4. Approve / Reject Requests");
        Console.Write("\nChoice: ");
        var subChoice = Console.ReadLine()?.Trim();

        if (!await CheckSubPermissionAsync(currentUser, input, subChoice))
            return;

        if (subChoice == "1")
            await _app.HolidayManager.CheckHolidaysAsync();
        else if (subChoice == "2")
            await _app.HolidayManager.RequestHolidayAsync(currentUser);
        else if (subChoice == "3")
            await _app.HolidayManager.CheckOwnHolidayBankAsync(currentUser);
        else if (subChoice == "4" && currentUser.Role == UserRole.HR)
            await _app.HolidayManager.SetupHolidayConfigAsync();
        else if (subChoice == "4" && currentUser.Role == UserRole.Manager)
            await _app.HolidayManager.ApproveRejectHolidayAsync(currentUser);
        else
            Console.WriteLine("Invalid choice.");
    }

    private async Task HandleTeamManagementAsync(AuthUser currentUser)
    {
        Console.WriteLine("\n  1. Create Team");
        Console.WriteLine("  2. Update Team");
        Console.WriteLine("  3. View All Teams");
        Console.Write("\nChoice: ");
        var subChoice = Console.ReadLine()?.Trim();

        if (!await CheckSubPermissionAsync(currentUser, "4", subChoice))
            return;

        if (subChoice == "1")
            await _app.TeamManager.CreateTeamAsync();
        else if (subChoice == "2")
            await _app.TeamManager.UpdateTeamAsync();
        else if (subChoice == "3")
            await _app.TeamManager.ViewAllTeamsAsync();
        else
            Console.WriteLine("Invalid choice.");
    }

    private async Task HandleManagerPerformanceAsync(AuthUser currentUser, Func<Permission, string, Task<bool>> scopeChecker)
    {
        Console.WriteLine("\n  1. Review Team Performance");
        Console.WriteLine("  2. Submit Own Performance Review");
        Console.WriteLine("  3. Check Own History");
        Console.Write("\nChoice: ");
        var subChoice = Console.ReadLine()?.Trim();

        if (!await CheckSubPermissionAsync(currentUser, "1", subChoice))
            return;

        if (subChoice == "1")
            await _app.PerformanceManager.ReviewTeamPerformanceAsync(currentUser, scopeChecker);
        else if (subChoice == "2")
            await _app.PerformanceManager.SubmitOwnReviewAsync(currentUser);
        else if (subChoice == "3")
            await _app.PerformanceManager.CheckOwnHistoryAsync(currentUser);
        else
            Console.WriteLine("Invalid choice.");
    }

    private async Task HandleManagerPayrollAsync(AuthUser currentUser, Func<Permission, string, Task<bool>> scopeChecker)
    {
        Console.WriteLine("\n  1. Check Reportee Salary");
        Console.WriteLine("  2. Check Own Salary");
        Console.Write("\nChoice: ");
        var subChoice = Console.ReadLine()?.Trim();

        if (!await CheckSubPermissionAsync(currentUser, "3", subChoice))
            return;

        if (subChoice == "1")
            await _app.SalaryLevelManager.CheckSomeonesSalaryAsync(currentUser, scopeChecker);
        else if (subChoice == "2")
            await _app.SalaryLevelManager.CheckOwnSalaryAsync(currentUser);
        else
            Console.WriteLine("Invalid choice.");
    }

    private async Task HandleEmployeePerformanceAsync(AuthUser currentUser)
    {
        Console.WriteLine("\n  1. Submit Performance Review");
        Console.WriteLine("  2. Check History");
        Console.Write("\nChoice: ");
        var subChoice = Console.ReadLine()?.Trim();

        if (!await CheckSubPermissionAsync(currentUser, "4", subChoice))
            return;

        if (subChoice == "1")
            await _app.PerformanceManager.SubmitOwnReviewAsync(currentUser);
        else if (subChoice == "2")
            await _app.PerformanceManager.CheckOwnHistoryAsync(currentUser);
        else
            Console.WriteLine("Invalid choice.");
    }

    private async Task HandleActiveSessionsAsync(AuthUser currentUser)
    {
        var activeSessions = await _app.SessionRepo.GetAllActiveSessionsAsync();
        if (activeSessions.Count == 0)
        {
            Console.WriteLine("\n  No active sessions.");
            return;
        }

        Console.WriteLine($"\n  Active Sessions ({activeSessions.Count}):\n");
        for (int i = 0; i < activeSessions.Count; i++)
        {
            var s = activeSessions[i];
            Console.WriteLine($"    {i + 1}. {s.Alias} ({s.Role}) — Login: {s.LoginTime:yyyy-MM-dd HH:mm} | Expires: {s.ExpiresAt:yyyy-MM-dd HH:mm}");
        }

        Console.Write("\n  Force logout a user? Enter number (0 to cancel): ");
        if (int.TryParse(Console.ReadLine()?.Trim(), out int sel) && sel > 0 && sel <= activeSessions.Count)
        {
            var target = activeSessions[sel - 1];
            if (target.Alias == currentUser.Alias)
                Console.WriteLine("  Cannot force logout yourself.");
            else
            {
                await _app.SessionRepo.DeactivateSessionAsync(target);
                Console.WriteLine($"  Session for {target.Alias} has been terminated.");
            }
        }
    }

    private async Task<bool> CheckAccessAsync(AuthUser user, Permission action, string? targetAlias = null)
    {
        var request = new AuthorizationRequest { User = user, Action = action, TargetAlias = targetAlias };
        var result = await _app.RuleEngine.EvaluateAsync(request);
        if (result.Status == RuleStatus.Denied)
        {
            Console.WriteLine($"  Access Denied: {result.Reason}");
            return false;
        }
        return true;
    }

    private async Task<bool> CheckSubPermissionAsync(AuthUser user, string input, string? subChoice)
    {
        var subPerm = _app.AuthzService.GetRequiredPermission(user.Role, input, subChoice);
        if (subPerm != null && !await CheckAccessAsync(user, subPerm.Value))
            return false;
        return true;
    }

    private static bool IsHolidayMenu(UserRole role, string input) =>
        (role == UserRole.HR && input == "3") ||
        (role == UserRole.Manager && input == "4") ||
        (role == UserRole.Employee && input == "1");

    private static bool IsUpdateInfoMenu(UserRole role, string input) =>
        (role == UserRole.HR && input == "6") ||
        (role == UserRole.Manager && input == "5") ||
        (role == UserRole.Employee && input == "3");

    private static bool IsHRBotMenu(UserRole role, string input) =>
        (role == UserRole.HR && input == "7") ||
        (role == UserRole.Manager && input == "6") ||
        (role == UserRole.Employee && input == "5");
}
