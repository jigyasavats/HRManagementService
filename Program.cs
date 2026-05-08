using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using HRManagementService.Repository;
using HRManagementService.AuthService;
using HRManagementService.EmployeeService;
using HRManagementService.TeamService;
using HRManagementService.PayrollService;
using HRManagementService.HolidayService;
using HRManagementService.Pipeline;
using HRManagementService.Enums;
using HRManagementService.PerformanceService;

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var vaultUrl = config["KeyVault:VaultUrl"];
var databaseId = config["CosmosDb:DatabaseId"];
var cosmosSecretName = config["CosmosDb:ConnectionStringSecretName"];
var serviceBusSecretName = config["ServiceBus:ConnectionStringSecretName"];

if (string.IsNullOrEmpty(vaultUrl) || string.IsNullOrEmpty(databaseId) || 
    string.IsNullOrEmpty(cosmosSecretName) || string.IsNullOrEmpty(serviceBusSecretName))
{
    Console.WriteLine("Error: Missing configuration values in appsettings.json.");
    return;
}

Console.WriteLine("Connecting to Azure Key Vault...");
var secretClient = new SecretClient(new Uri(vaultUrl), new DefaultAzureCredential());

string cosmosConnectionString;
string serviceBusConnectionString;
try
{
    var cosmosSecret = await secretClient.GetSecretAsync(cosmosSecretName);
    cosmosConnectionString = cosmosSecret.Value.Value;

    var serviceBusSecret = await secretClient.GetSecretAsync(serviceBusSecretName);
    serviceBusConnectionString = serviceBusSecret.Value.Value;

    Console.WriteLine("Secrets retrieved successfully.");
}
catch (Exception ex)
{
    Console.WriteLine($"Error fetching secrets: {ex.Message}");
    return;
}

Console.WriteLine("Initializing Cosmos DB...");
var cosmosService = await CosmosDbService.InitializeAsync(cosmosConnectionString, databaseId);

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

Console.WriteLine("Containers ready.");

var authRepo = new AuthRepository(usersContainer);
var employeeRepo = new EmployeeRepository(employeesContainer);
var teamRepo = new TeamRepository(teamsContainer);
var payrollRepo = new PayrollRepository(levelSalaryContainer, payrollContainer);
var holidayRepo = new HolidayRepository(holidayConfigContainer, holidayBankContainer);
var performanceRepo = new PerformanceRepository(performanceContainer);
var auditRepo = new AuditRepository(auditContainer);
var onboardingRepo = new OnboardingRepository(onboardingContainer);

var serviceBus = new ServiceBusService(serviceBusConnectionString);

var authManager = new AuthManager(authRepo, employeeRepo);

var queueNames = new Dictionary<string, string>
{
    ["EmployeeOnboarding"] = config["ServiceBus:Queues:EmployeeOnboarding"]!,
    ["PayrollOperations"] = config["ServiceBus:Queues:PayrollOperations"]!,
    ["HolidayRequests"] = config["ServiceBus:Queues:HolidayRequests"]!,
    ["PerformanceReviews"] = config["ServiceBus:Queues:PerformanceReviews"]!,
    ["TeamOperations"] = config["ServiceBus:Queues:TeamOperations"]!
};

var employeePipeline = new EmployeePipeline(
    employeeRepo, teamRepo, payrollRepo, holidayRepo,
    performanceRepo, authRepo, auditRepo, onboardingRepo, serviceBus, queueNames);

var employeeManager = new EmployeeManager(teamRepo, payrollRepo, holidayRepo, onboardingRepo, employeeRepo, authRepo, employeePipeline);
var teamManager = new TeamManager(teamRepo, authRepo);
var salaryLevelManager = new SalaryLevelManager(payrollRepo, employeeRepo, teamRepo);
var performanceManager = new PerformanceManager(performanceRepo);
var holidayManager = new HolidayManager(holidayRepo, employeeRepo, teamRepo);

Console.WriteLine("\n========================================");
Console.WriteLine("   HR Management Service");
Console.WriteLine("========================================\n");

var currentUser = await authManager.LoginAsync();
if (currentUser == null)
{
    Console.WriteLine("Login failed. Exiting.");
    return;
}

while (true)
{
    Console.WriteLine("\n========================================");
    Console.WriteLine($"  Menu ({currentUser.Role})");
    Console.WriteLine("========================================");

    if (currentUser.Role == UserRole.HR)
    {
        Console.WriteLine("  1.  Employee Onboarding");
        Console.WriteLine("  2.  Give Promotion / Raise        [Coming Soon]");
        Console.WriteLine("  3.  Fire Employee                 [Coming Soon]");
        Console.WriteLine("  4.  Setup Salary Levels");
        Console.WriteLine("  5.  Holidays");
        Console.WriteLine("  6.  Team Management");
        Console.WriteLine("  7.  View Audit Logs               [Coming Soon]");
        Console.WriteLine("  8.  Check Employee Performance    [Coming Soon]");
        Console.WriteLine("  9.  Check Someone's Salary");
        Console.WriteLine("  10. Update Personal Info");
        Console.WriteLine("  11. Submit Own Performance Review");
        Console.WriteLine("  12. Exit");
    }
    else if (currentUser.Role == UserRole.Manager)
    {
        Console.WriteLine("  1.  Check Employee Performance    [Coming Soon]");
        Console.WriteLine("  2.  Add Reportee Performance Review [Coming Soon]");
        Console.WriteLine("  3.  Flag Employee for Termination [Coming Soon]");
        Console.WriteLine("  4.  Team Management");
        Console.WriteLine("  5.  Check Someone's Salary");
        Console.WriteLine("  6.  Check Own Salary");
        Console.WriteLine("  7.  Check Readiness for Next Step [Coming Soon]");
        Console.WriteLine("  8.  Holidays");
        Console.WriteLine("  9.  Update Personal Info");
        Console.WriteLine("  10. Submit Own Performance Review");
        Console.WriteLine("  11. Exit");
    }
    else
    {
        Console.WriteLine("  1. Holidays");
        Console.WriteLine("  2. Check Own Salary");
        Console.WriteLine("  3. Update Personal Info");
        Console.WriteLine("  4. Submit Own Performance Review");
        Console.WriteLine("  5. Exit");
    }

    Console.Write("\nChoice: ");
    var input = Console.ReadLine()?.Trim();

    var exitOption = currentUser.Role switch
    {
        UserRole.HR => "12",
        UserRole.Manager => "11",
        UserRole.Employee => "5",
        _ => "1"
    };

    if (input == exitOption)
    {
        Console.WriteLine("\nGoodbye!");
        break;
    }

    if (currentUser.Role == UserRole.HR && input == "1")
    {
        Console.WriteLine("\n  1. Add New Employee");
        Console.WriteLine("  2. Check Onboarding Status");
        Console.Write("\nChoice: ");
        var subChoice = Console.ReadLine()?.Trim();
        if (subChoice == "1")
            await employeeManager.AddNewEmployeeAsync(currentUser);
        else if (subChoice == "2")
            await employeeManager.CheckOnboardingStatusAsync();
        else
            Console.WriteLine("Invalid choice.");
    }
    else if (currentUser.Role == UserRole.HR && input == "4")
    {
        await salaryLevelManager.SetupSalaryLevelsAsync();
    }
    else if ((currentUser.Role == UserRole.HR && input == "5") ||
             (currentUser.Role == UserRole.Manager && input == "8") ||
             (currentUser.Role == UserRole.Employee && input == "1"))
    {
        Console.WriteLine("\n  1. View Fixed Holidays");
        Console.WriteLine("  2. Request Holiday");
        Console.WriteLine("  3. Check Own Holiday Bank");
        if (currentUser.Role == UserRole.HR)
            Console.WriteLine("  4. Setup Holiday Config");
        if (currentUser.Role == UserRole.Manager)
            Console.WriteLine("  4. Approve / Reject Requests");
        Console.Write("\nChoice: ");
        var subChoice = Console.ReadLine()?.Trim();
        if (subChoice == "1")
            await holidayManager.CheckHolidaysAsync();
        else if (subChoice == "2")
            await holidayManager.RequestHolidayAsync(currentUser);
        else if (subChoice == "3")
            await holidayManager.CheckOwnHolidayBankAsync(currentUser);
        else if (subChoice == "4" && currentUser.Role == UserRole.HR)
            await holidayManager.SetupHolidayConfigAsync();
        else if (subChoice == "4" && currentUser.Role == UserRole.Manager)
            await holidayManager.ApproveRejectHolidayAsync(currentUser);
        else
            Console.WriteLine("Invalid choice.");
    }
    else if ((currentUser.Role == UserRole.HR && input == "6") ||
             (currentUser.Role == UserRole.Manager && input == "4"))
    {
        Console.WriteLine("\n  1. Create Team");
        Console.WriteLine("  2. Update Team");
        Console.WriteLine("  3. View All Teams");
        Console.Write("\nChoice: ");
        var subChoice = Console.ReadLine()?.Trim();
        if (subChoice == "1")
            await teamManager.CreateTeamAsync();
        else if (subChoice == "2")
            await teamManager.UpdateTeamAsync();
        else if (subChoice == "3")
            await teamManager.ViewAllTeamsAsync();
        else
            Console.WriteLine("Invalid choice.");
    }
    else if ((currentUser.Role == UserRole.HR && input == "10") ||
             (currentUser.Role == UserRole.Manager && input == "9") ||
             (currentUser.Role == UserRole.Employee && input == "3"))
    {
        await employeeManager.UpdatePersonalInfoAsync(currentUser);
    }
    else if ((currentUser.Role == UserRole.HR && input == "9") ||
             (currentUser.Role == UserRole.Manager && input == "5"))
    {
        await salaryLevelManager.CheckSomeonesSalaryAsync(currentUser);
    }
    else if ((currentUser.Role == UserRole.Manager && input == "6") ||
             (currentUser.Role == UserRole.Employee && input == "2"))
    {
        await salaryLevelManager.CheckOwnSalaryAsync(currentUser);
    }
    else if ((currentUser.Role == UserRole.HR && input == "11") ||
             (currentUser.Role == UserRole.Manager && input == "10") ||
             (currentUser.Role == UserRole.Employee && input == "4"))
    {
        await performanceManager.SubmitOwnReviewAsync(currentUser);
    }
    else
    {
        Console.WriteLine("This feature is coming soon!");
    }
}
