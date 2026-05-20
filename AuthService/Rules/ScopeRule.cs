using HRManagementService.Enums;
using HRManagementService.Repository;

namespace HRManagementService.AuthService.Rules;

public class ScopeRule : IAuthorizationRule
{
    private readonly TeamRepository _teamRepo;
    private readonly EmployeeRepository _employeeRepo;

    public ScopeRule(TeamRepository teamRepo, EmployeeRepository employeeRepo)
    {
        _teamRepo = teamRepo;
        _employeeRepo = employeeRepo;
    }

    public async Task<RuleResult> EvaluateAsync(AuthorizationRequest request)
    {
        if (string.IsNullOrEmpty(request.TargetAlias))
            return RuleResult.Skip();

        var scope = GetScopeForRole(request.User.Role);

        switch (scope)
        {
            case ScopeType.All:
                return RuleResult.Allow();

            case ScopeType.Self:
                if (string.Equals(request.User.Alias, request.TargetAlias, StringComparison.OrdinalIgnoreCase))
                    return RuleResult.Allow();
                return RuleResult.Deny("You can only access your own records.");

            case ScopeType.TeamAndSelf:
                if (string.Equals(request.User.Alias, request.TargetAlias, StringComparison.OrdinalIgnoreCase))
                    return RuleResult.Allow();

                if (await IsInMyTeamAsync(request.User.Alias, request.TargetAlias))
                    return RuleResult.Allow();

                return RuleResult.Deny($"'{request.TargetAlias}' is not in your team.");

            default:
                return RuleResult.Deny("Unknown scope type.");
        }
    }

    private ScopeType GetScopeForRole(UserRole role)
    {
        return role switch
        {
            UserRole.HR => ScopeType.All,
            UserRole.Manager => ScopeType.TeamAndSelf,
            UserRole.Employee => ScopeType.Self,
            _ => ScopeType.Self
        };
    }

    private async Task<bool> IsInMyTeamAsync(string managerAlias, string targetAlias)
    {
        var manager = await _employeeRepo.GetByAliasAsync(managerAlias);
        if (manager == null) return false;

        var target = await _employeeRepo.GetByAliasAsync(targetAlias);
        if (target == null) return false;

        var team = await _teamRepo.GetByTeamIdAsync(manager.TeamId);
        if (team == null) return false;

        return team.ManagerId == manager.Id && team.EmployeeIds.Contains(target.Id);
    }
}
