using HRManagementService.Enums;
using HRManagementService.Models;
using HRManagementService.Repository;

namespace HRManagementService.AuthService;

public class AuthorizationService
{
    private readonly RolePermissionRepository _repo;
    private Dictionary<string, HashSet<string>> _cache = new();

    public AuthorizationService(RolePermissionRepository repo)
    {
        _repo = repo;
    }

    // Load all role-permissions from DB into memory cache
    public async Task LoadPermissionsAsync()
    {
        var all = await _repo.GetAllAsync();
        _cache = all.ToDictionary(
            r => r.Role,
            r => r.Permissions.ToHashSet()
        );
        Console.WriteLine($"  Loaded permissions for {_cache.Count} roles.");
    }

    public bool HasPermission(AuthUser user, Permission permission)
    {
        var roleName = user.Role.ToString();
        if (!_cache.ContainsKey(roleName))
            return false;

        return _cache[roleName].Contains(permission.ToString());
    }

    // Maps (role + menu input) to the required Permission
    // Returns null for logout/exit or unknown options (no permission needed)
    public Permission? GetRequiredPermission(UserRole role, string input, string? subChoice = null)
    {
        return (role, input, subChoice) switch
        {
            // HR menu
            (UserRole.HR, "1", "1") => Permission.AddEmployee,
            (UserRole.HR, "1", "2") => Permission.TerminateEmployee,
            (UserRole.HR, "1", "3") => Permission.ReviewPromotion,
            (UserRole.HR, "1", "4") => Permission.CheckPipelineStatus,
            (UserRole.HR, "2", _)   => Permission.SetupSalaryLevels,
            (UserRole.HR, "5", _)   => Permission.CheckAnySalary,
            (UserRole.HR, "8", _)   => Permission.ManageActiveSessions,

            // Manager menu
            (UserRole.Manager, "1", "1") => Permission.ReviewTeamPerformance,
            (UserRole.Manager, "1", "2") => Permission.SubmitOwnReview,
            (UserRole.Manager, "1", "3") => Permission.CheckOwnHistory,
            (UserRole.Manager, "2", _)   => Permission.ProposePromotion,
            (UserRole.Manager, "3", "1") => Permission.CheckAnySalary,
            (UserRole.Manager, "3", "2") => Permission.CheckOwnSalary,

            // Employee menu
            (UserRole.Employee, "2", _) => Permission.CheckOwnSalary,
            (UserRole.Employee, "4", "1") => Permission.SubmitOwnReview,
            (UserRole.Employee, "4", "2") => Permission.CheckOwnHistory,

            // Shared — Holidays (HR=3, Manager=4, Employee=1)
            (_, _, "1") when IsHolidayMenu(role, input) => Permission.ViewHolidays,
            (_, _, "2") when IsHolidayMenu(role, input) => Permission.RequestHoliday,
            (_, _, "3") when IsHolidayMenu(role, input) => Permission.CheckOwnHolidayBank,
            (UserRole.HR, "3", "4")      => Permission.SetupHolidayConfig,
            (UserRole.Manager, "4", "4") => Permission.ApproveRejectHoliday,

            // Shared — Update Personal Info (HR=6, Manager=5, Employee=3)
            (UserRole.HR, "6", _)       => Permission.UpdatePersonalInfo,
            (UserRole.Manager, "5", _)  => Permission.UpdatePersonalInfo,
            (UserRole.Employee, "3", _) => Permission.UpdatePersonalInfo,

            // Shared — Ask HR Bot (HR=7, Manager=6, Employee=5)
            (UserRole.HR, "7", _)       => Permission.AskHRBot,
            (UserRole.Manager, "6", _)  => Permission.AskHRBot,
            (UserRole.Employee, "5", _) => Permission.AskHRBot,

            // Team Management (HR=4)
            (UserRole.HR, "4", "1") => Permission.CreateTeam,
            (UserRole.HR, "4", "2") => Permission.UpdateTeam,
            (UserRole.HR, "4", "3") => Permission.ViewAllTeams,

            _ => null
        };
    }

    private static bool IsHolidayMenu(UserRole role, string input)
    {
        return (role == UserRole.HR && input == "3") ||
               (role == UserRole.Manager && input == "4") ||
               (role == UserRole.Employee && input == "1");
    }

    // Seed default permissions if DB is empty
    public async Task SeedDefaultsIfEmptyAsync()
    {
        var existing = await _repo.GetAllAsync();
        if (existing.Count > 0) return;

        Console.WriteLine("  Seeding default role permissions...");

        var hrPermissions = new RolePermission
        {
            Id = "HR",
            Role = "HR",
            Permissions = new List<string>
            {
                nameof(Permission.AddEmployee),
                nameof(Permission.TerminateEmployee),
                nameof(Permission.ReviewPromotion),
                nameof(Permission.CheckPipelineStatus),
                nameof(Permission.SetupSalaryLevels),
                nameof(Permission.CheckAnySalary),
                nameof(Permission.CheckOwnSalary),
                nameof(Permission.ViewHolidays),
                nameof(Permission.RequestHoliday),
                nameof(Permission.CheckOwnHolidayBank),
                nameof(Permission.SetupHolidayConfig),
                nameof(Permission.CreateTeam),
                nameof(Permission.UpdateTeam),
                nameof(Permission.ViewAllTeams),
                nameof(Permission.UpdatePersonalInfo),
                nameof(Permission.AskHRBot),
                nameof(Permission.ManageActiveSessions)
            }
        };

        var managerPermissions = new RolePermission
        {
            Id = "Manager",
            Role = "Manager",
            Permissions = new List<string>
            {
                nameof(Permission.ReviewTeamPerformance),
                nameof(Permission.SubmitOwnReview),
                nameof(Permission.CheckOwnHistory),
                nameof(Permission.ProposePromotion),
                nameof(Permission.CheckAnySalary),
                nameof(Permission.CheckOwnSalary),
                nameof(Permission.ViewHolidays),
                nameof(Permission.RequestHoliday),
                nameof(Permission.CheckOwnHolidayBank),
                nameof(Permission.ApproveRejectHoliday),
                nameof(Permission.UpdatePersonalInfo),
                nameof(Permission.AskHRBot)
            }
        };

        var employeePermissions = new RolePermission
        {
            Id = "Employee",
            Role = "Employee",
            Permissions = new List<string>
            {
                nameof(Permission.SubmitOwnReview),
                nameof(Permission.CheckOwnHistory),
                nameof(Permission.CheckOwnSalary),
                nameof(Permission.ViewHolidays),
                nameof(Permission.RequestHoliday),
                nameof(Permission.CheckOwnHolidayBank),
                nameof(Permission.UpdatePersonalInfo),
                nameof(Permission.AskHRBot)
            }
        };

        await _repo.UpsertAsync(hrPermissions);
        await _repo.UpsertAsync(managerPermissions);
        await _repo.UpsertAsync(employeePermissions);

        Console.WriteLine("  Default permissions seeded.");
    }
}
