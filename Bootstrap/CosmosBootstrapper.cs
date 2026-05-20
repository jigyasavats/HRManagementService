using Microsoft.Extensions.Configuration;
using HRManagementService.Repository;

namespace HRManagementService.Bootstrap;

public class Repositories
{
    public required AuthRepository AuthRepo { get; init; }
    public required EmployeeRepository EmployeeRepo { get; init; }
    public required TeamRepository TeamRepo { get; init; }
    public required PayrollRepository PayrollRepo { get; init; }
    public required HolidayRepository HolidayRepo { get; init; }
    public required PerformanceRepository PerformanceRepo { get; init; }
    public required AuditRepository AuditRepo { get; init; }
    public required OnboardingRepository OnboardingRepo { get; init; }
    public required PromotionRepository PromotionRepo { get; init; }
    public required SessionRepository SessionRepo { get; init; }
    public required RolePermissionRepository RolePermissionRepo { get; init; }
}

public static class CosmosBootstrapper
{
    public static async Task<Repositories> InitializeAsync(string connectionString, IConfiguration config)
    {
        var databaseId = config["CosmosDb:DatabaseId"]!;

        Console.WriteLine("Initializing Cosmos DB...");
        var cosmosService = await CosmosDbService.InitializeAsync(connectionString, databaseId);

        var usersContainer = await cosmosService.GetOrCreateContainerAsync(config["CosmosDb:Containers:Users"]!, "/email");
        var employeesContainer = await cosmosService.GetOrCreateContainerAsync(config["CosmosDb:Containers:Employees"]!, "/id");
        var teamsContainer = await cosmosService.GetOrCreateContainerAsync(config["CosmosDb:Containers:Teams"]!, "/teamId");
        var levelSalaryContainer = await cosmosService.GetOrCreateContainerAsync(config["CosmosDb:Containers:LevelSalaryRange"]!, "/level");
        var payrollContainer = await cosmosService.GetOrCreateContainerAsync(config["CosmosDb:Containers:EmployeePayroll"]!, "/employeeId");
        var holidayConfigContainer = await cosmosService.GetOrCreateContainerAsync(config["CosmosDb:Containers:HolidayConfig"]!, "/id");
        var holidayBankContainer = await cosmosService.GetOrCreateContainerAsync(config["CosmosDb:Containers:EmployeeHolidayBank"]!, "/employeeId");
        var performanceContainer = await cosmosService.GetOrCreateContainerAsync(config["CosmosDb:Containers:EmployeePerformance"]!, "/alias");
        var auditContainer = await cosmosService.GetOrCreateContainerAsync(config["CosmosDb:Containers:AuditLogs"]!, "/performedBy");
        var onboardingContainer = await cosmosService.GetOrCreateContainerAsync(config["CosmosDb:Containers:OnboardingStatus"]!, "/id");
        var promotionContainer = await cosmosService.GetOrCreateContainerAsync("PromotionRequests", "/alias");
        var sessionsContainer = await cosmosService.GetOrCreateContainerAsync(config["CosmosDb:Containers:Sessions"]!, "/alias");
        var rolePermissionsContainer = await cosmosService.GetOrCreateContainerAsync(config["CosmosDb:Containers:RolePermissions"]!, "/role");

        Console.WriteLine("Containers ready.");

        return new Repositories
        {
            AuthRepo = new AuthRepository(usersContainer),
            EmployeeRepo = new EmployeeRepository(employeesContainer),
            TeamRepo = new TeamRepository(teamsContainer),
            PayrollRepo = new PayrollRepository(levelSalaryContainer, payrollContainer),
            HolidayRepo = new HolidayRepository(holidayConfigContainer, holidayBankContainer),
            PerformanceRepo = new PerformanceRepository(performanceContainer),
            AuditRepo = new AuditRepository(auditContainer),
            OnboardingRepo = new OnboardingRepository(onboardingContainer),
            PromotionRepo = new PromotionRepository(promotionContainer),
            SessionRepo = new SessionRepository(sessionsContainer),
            RolePermissionRepo = new RolePermissionRepository(rolePermissionsContainer)
        };
    }
}
