using HRManagementService.AuthService;
using HRManagementService.AuthService.Rules;
using HRManagementService.EmployeeService;
using HRManagementService.TeamService;
using HRManagementService.PayrollService;
using HRManagementService.HolidayService;
using HRManagementService.PerformanceService;
using HRManagementService.AIService;
using HRManagementService.Repository;

namespace HRManagementService.Bootstrap;

public class AppServices
{
    public required AuthManager AuthManager { get; init; }
    public required AuthorizationService AuthzService { get; init; }
    public required RuleEngine RuleEngine { get; init; }
    public required JwtService JwtService { get; init; }
    public required SessionRepository SessionRepo { get; init; }
    public required EmployeeManager EmployeeManager { get; init; }
    public required TeamManager TeamManager { get; init; }
    public required SalaryLevelManager SalaryLevelManager { get; init; }
    public required PerformanceManager PerformanceManager { get; init; }
    public required HolidayManager HolidayManager { get; init; }
    public required AIManager AIManager { get; init; }
}
