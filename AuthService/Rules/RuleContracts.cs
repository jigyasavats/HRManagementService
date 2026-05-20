using HRManagementService.Enums;
using HRManagementService.Models;

namespace HRManagementService.AuthService.Rules;

public class AuthorizationRequest
{
    public AuthUser User { get; set; } = null!;
    public Permission Action { get; set; }
    public string? TargetAlias { get; set; }
}

public class RuleResult
{
    public RuleStatus Status { get; set; }
    public string? Reason { get; set; }

    public static RuleResult Allow() => new() { Status = RuleStatus.Allowed };
    public static RuleResult Deny(string reason) => new() { Status = RuleStatus.Denied, Reason = reason };
    public static RuleResult Skip() => new() { Status = RuleStatus.Skipped };
}

public interface IAuthorizationRule
{
    Task<RuleResult> EvaluateAsync(AuthorizationRequest request);
}
