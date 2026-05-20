using HRManagementService.Bootstrap;
using HRManagementService.Enums;
using HRManagementService.Models;
using HRManagementService.AuthService.Rules;

AppServices app;
try
{
    app = await AppBuilder.BuildAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"Startup failed: {ex.Message}");
    return;
}

var menuRouter = new HRManagementService.MenuRouter(app);

while (true)
{
    Console.WriteLine("\n========================================");
    Console.WriteLine("   HR Management Service");
    Console.WriteLine("========================================\n");

    var currentUser = await app.AuthManager.LoginAsync();
    if (currentUser == null)
    {
        Console.WriteLine("Login failed. Exiting.");
        return;
    }

    var token = app.JwtService.GenerateToken(currentUser.Alias, currentUser.Name, currentUser.Role);
    var tokenId = app.JwtService.GetClaim(token, "jti")!;

    var userSession = new UserSession
    {
        Id = tokenId,
        Alias = currentUser.Alias,
        Role = currentUser.Role.ToString(),
        TokenId = tokenId,
        LoginTime = DateTime.UtcNow,
        ExpiresAt = DateTime.UtcNow.AddMinutes(30)
    };
    await app.SessionRepo.CreateSessionAsync(userSession);
    Console.WriteLine($"  Session started (expires in 30 min)");

    Func<Permission, string, Task<bool>> scopeChecker = async (action, targetAlias) =>
    {
        var request = new AuthorizationRequest { User = currentUser, Action = action, TargetAlias = targetAlias };
        var result = await app.RuleEngine.EvaluateAsync(request);
        if (result.Status == RuleStatus.Denied)
        {
            Console.WriteLine($"  Access Denied: {result.Reason}");
            return false;
        }
        return true;
    };

    bool loggedOut = false;

    while (!loggedOut)
    {
        if (!app.JwtService.IsTokenValid(token))
        {
            await app.SessionRepo.DeactivateSessionAsync(userSession);
            Console.WriteLine("\n  Session expired. Please login again.");
            break;
        }

        var activeSession = await app.SessionRepo.GetActiveSessionAsync(currentUser.Alias);
        if (activeSession == null || activeSession.TokenId != userSession.TokenId)
        {
            Console.WriteLine("\n  Your session was terminated. Please login again.");
            break;
        }

        menuRouter.ShowMenu(currentUser.Role);
        Console.Write("\nChoice: ");
        var input = Console.ReadLine()?.Trim();

        if (input == menuRouter.GetLogoutOption(currentUser.Role))
        {
            await app.SessionRepo.DeactivateSessionAsync(userSession);
            Console.WriteLine($"\nSession ended. Goodbye, {currentUser.Name}!");
            loggedOut = true;
            continue;
        }

        if (input == menuRouter.GetExitOption(currentUser.Role))
        {
            await app.SessionRepo.DeactivateSessionAsync(userSession);
            Console.WriteLine("\nSession ended. Goodbye!");
            return;
        }

        await menuRouter.HandleActionAsync(input!, currentUser, scopeChecker);
    }
}
