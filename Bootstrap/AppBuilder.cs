using HRManagementService.AuthService;
using HRManagementService.AuthService.Rules;
using HRManagementService.EmployeeService;
using HRManagementService.TeamService;
using HRManagementService.PayrollService;
using HRManagementService.HolidayService;
using HRManagementService.PerformanceService;
using HRManagementService.Pipeline;
using HRManagementService.AIService;

namespace HRManagementService.Bootstrap;

public static class AppBuilder
{
    public static async Task<AppServices> BuildAsync()
    {
        var config = ConfigLoader.Load();
        var secrets = await KeyVaultBootstrapper.LoadSecretsAsync(config);
        var repos = await CosmosBootstrapper.InitializeAsync(secrets.CosmosConnectionString, config);

        var serviceBus = ServiceBusBootstrapper.Initialize(secrets.ServiceBusConnectionString);
        var queueNames = ServiceBusBootstrapper.LoadQueueNames(config);

        var aiManager = new AIManager(secrets.OpenAiEndpoint, secrets.OpenAiApiKey, config["OpenAI:DeploymentName"]!);
        var authManager = new AuthManager(repos.AuthRepo, repos.EmployeeRepo);

        var authzService = new AuthorizationService(repos.RolePermissionRepo);
        await authzService.SeedDefaultsIfEmptyAsync();
        await authzService.LoadPermissionsAsync();

        var ruleEngine = new RuleEngine(new List<IAuthorizationRule>
        {
            new PermissionRule(authzService),
            new ScopeRule(repos.TeamRepo, repos.EmployeeRepo),
            new StateRule(repos.EmployeeRepo)
        });

        // REPLACE: Use a strong secret (min 32 chars). Store in Key Vault for production.
        var jwtSecret = "<your-jwt-secret-min-32-chars>";
        var jwtService = new JwtService(jwtSecret, expiryMinutes: 30);

        var employeePipeline = new EmployeePipeline(
            repos.EmployeeRepo, repos.TeamRepo, repos.PayrollRepo, repos.HolidayRepo,
            repos.AuthRepo, repos.AuditRepo, repos.OnboardingRepo, serviceBus, queueNames);

        var offboardingPipeline = new OffboardingPipeline(
            repos.EmployeeRepo, repos.TeamRepo, repos.PayrollRepo,
            repos.AuthRepo, repos.AuditRepo, repos.OnboardingRepo, serviceBus, queueNames);

        var employeeManager = new EmployeeManager(
            repos.TeamRepo, repos.PayrollRepo, repos.HolidayRepo, repos.OnboardingRepo,
            repos.EmployeeRepo, repos.AuthRepo, repos.PerformanceRepo, repos.PromotionRepo,
            employeePipeline, offboardingPipeline, serviceBus, repos.AuditRepo, aiManager, queueNames);

        var teamManager = new TeamManager(repos.TeamRepo, repos.AuthRepo);
        var salaryLevelManager = new SalaryLevelManager(repos.PayrollRepo, repos.EmployeeRepo, repos.TeamRepo);
        var performanceManager = new PerformanceManager(repos.PerformanceRepo, repos.TeamRepo, repos.AuthRepo, aiManager);
        var holidayManager = new HolidayManager(repos.HolidayRepo, repos.EmployeeRepo, repos.TeamRepo);

        return new AppServices
        {
            AuthManager = authManager,
            AuthzService = authzService,
            RuleEngine = ruleEngine,
            JwtService = jwtService,
            SessionRepo = repos.SessionRepo,
            EmployeeManager = employeeManager,
            TeamManager = teamManager,
            SalaryLevelManager = salaryLevelManager,
            PerformanceManager = performanceManager,
            HolidayManager = holidayManager,
            AIManager = aiManager
        };
    }
}
