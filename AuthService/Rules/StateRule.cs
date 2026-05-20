using HRManagementService.Repository;

namespace HRManagementService.AuthService.Rules;

public class StateRule : IAuthorizationRule
{
    private readonly EmployeeRepository _employeeRepo;

    public StateRule(EmployeeRepository employeeRepo)
    {
        _employeeRepo = employeeRepo;
    }

    public async Task<RuleResult> EvaluateAsync(AuthorizationRequest request)
    {
        if (string.IsNullOrEmpty(request.TargetAlias))
            return RuleResult.Skip();

        var target = await _employeeRepo.GetByAliasAsync(request.TargetAlias);

        if (target == null)
            return RuleResult.Deny($"Employee '{request.TargetAlias}' not found.");

        if (target.Status != "Active")
            return RuleResult.Deny($"Employee '{request.TargetAlias}' is not active (Status: {target.Status}).");

        return RuleResult.Allow();
    }
}
