using HRManagementService.Enums;

namespace HRManagementService.AuthService.Rules;

public class PermissionRule : IAuthorizationRule
{
    private readonly AuthorizationService _authService;

    public PermissionRule(AuthorizationService authService)
    {
        _authService = authService;
    }

    public Task<RuleResult> EvaluateAsync(AuthorizationRequest request)
    {
        if (_authService.HasPermission(request.User, request.Action))
            return Task.FromResult(RuleResult.Allow());

        return Task.FromResult(RuleResult.Deny(
            $"Role '{request.User.Role}' does not have permission '{request.Action}'."));
    }
}
