using HRManagementService.Enums;

namespace HRManagementService.AuthService.Rules;

public class RuleEngine
{
    private readonly List<IAuthorizationRule> _rules;

    public RuleEngine(List<IAuthorizationRule> rules)
    {
        _rules = rules;
    }

    public async Task<RuleResult> EvaluateAsync(AuthorizationRequest request)
    {
        foreach (var rule in _rules)
        {
            var result = await rule.EvaluateAsync(request);

            if (result.Status == RuleStatus.Denied)
                return result;

            if (result.Status == RuleStatus.Allowed)
                continue;

            // Skipped — move to next rule
        }

        return RuleResult.Allow();
    }
}
