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
using HRManagementService.AIService;

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var vaultUrl = config["KeyVault:VaultUrl"];
var databaseId = config["CosmosDb:DatabaseId"];
var cosmosSecretName = config["CosmosDb:ConnectionStringSecretName"];
var serviceBusSecretName = config["ServiceBus:ConnectionStringSecretName"];
var openAiKeySecretName = config["OpenAI:ApiKeySecretName"];
var openAiEndpointSecretName = config["OpenAI:EndpointSecretName"];
var openAiDeploymentName = config["OpenAI:DeploymentName"];

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
string openAiApiKey;
string openAiEndpoint;
try
{
    var cosmosSecret = await secretClient.GetSecretAsync(cosmosSecretName);
    cosmosConnectionString = cosmosSecret.Value.Value;

    var serviceBusSecret = await secretClient.GetSecretAsync(serviceBusSecretName);
    serviceBusConnectionString = serviceBusSecret.Value.Value;

    var openAiKeySecret = await secretClient.GetSecretAsync(openAiKeySecretName);
    openAiApiKey = openAiKeySecret.Value.Value;

    var openAiEndpointSecret = await secretClient.GetSecretAsync(openAiEndpointSecretName);
    openAiEndpoint = openAiEndpointSecret.Value.Value;

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
var promotionContainer = await cosmosService.GetOrCreateContainerAsync("PromotionRequests", "/alias");

Console.WriteLine("Containers ready.");

var authRepo = new AuthRepository(usersContainer);
var employeeRepo = new EmployeeRepository(employeesContainer);
var teamRepo = new TeamRepository(teamsContainer);
var payrollRepo = new PayrollRepository(levelSalaryContainer, payrollContainer);
var holidayRepo = new HolidayRepository(holidayConfigContainer, holidayBankContainer);
var performanceRepo = new PerformanceRepository(performanceContainer);
var auditRepo = new AuditRepository(auditContainer);
var onboardingRepo = new OnboardingRepository(onboardingContainer);
var promotionRepo = new PromotionRepository(promotionContainer);

var serviceBus = new ServiceBusService(serviceBusConnectionString);

var aiManager = new AIManager(openAiEndpoint, openAiApiKey, openAiDeploymentName!);

var authManager = new AuthManager(authRepo, employeeRepo);

var queueNames = new Dictionary<string, string>
{
    ["EmployeeOnboarding"] = config["ServiceBus:Queues:EmployeeOnboarding"]!,
    ["EmployeeOffboarding"] = config["ServiceBus:Queues:EmployeeOffboarding"]!,
    ["PromotionRaise"] = config["ServiceBus:Queues:PromotionRaise"]!,
    ["PayrollOperations"] = config["ServiceBus:Queues:PayrollOperations"]!,
    ["HolidayRequests"] = config["ServiceBus:Queues:HolidayRequests"]!,
    ["PerformanceReviews"] = config["ServiceBus:Queues:PerformanceReviews"]!,
    ["TeamOperations"] = config["ServiceBus:Queues:TeamOperations"]!
};

var employeePipeline = new EmployeePipeline(
    employeeRepo, teamRepo, payrollRepo, holidayRepo,
    authRepo, auditRepo, onboardingRepo, serviceBus, queueNames);

var offboardingPipeline = new OffboardingPipeline(
    employeeRepo, teamRepo, payrollRepo,
    authRepo, auditRepo, onboardingRepo, serviceBus, queueNames);

var employeeManager = new EmployeeManager(teamRepo, payrollRepo, holidayRepo, onboardingRepo, employeeRepo, authRepo, performanceRepo, promotionRepo, employeePipeline, offboardingPipeline, serviceBus, auditRepo, aiManager, queueNames);
var teamManager = new TeamManager(teamRepo, authRepo);
var salaryLevelManager = new SalaryLevelManager(payrollRepo, employeeRepo, teamRepo);
var performanceManager = new PerformanceManager(performanceRepo, teamRepo, authRepo, aiManager);
var holidayManager = new HolidayManager(holidayRepo, employeeRepo, teamRepo);

while (true)
{
    Console.WriteLine("\n========================================");
    Console.WriteLine("   HR Management Service");
    Console.WriteLine("========================================\n");

    var currentUser = await authManager.LoginAsync();
    if (currentUser == null)
    {
        Console.WriteLine("Login failed. Exiting.");
        return;
    }

    bool loggedOut = false;

    while (!loggedOut)
    {
        Console.WriteLine("\n========================================");
        Console.WriteLine($"  Menu ({currentUser.Role})");
        Console.WriteLine("========================================");

        if (currentUser.Role == UserRole.HR)
        {
            Console.WriteLine("  1.  Employee Actions");
            Console.WriteLine("  2.  Setup Salary Levels");
            Console.WriteLine("  3.  Holidays");
            Console.WriteLine("  4.  Team Management");
            Console.WriteLine("  5.  Payroll");
            Console.WriteLine("  6.  Update Personal Info");
            Console.WriteLine("  7.  Ask HR Bot");
            Console.WriteLine("  8.  Logout");
            Console.WriteLine("  9.  Exit");
        }
        else if (currentUser.Role == UserRole.Manager)
        {
            Console.WriteLine("  1.  Performance Reviews");
            Console.WriteLine("  2.  Propose Promotion");
            Console.WriteLine("  3.  Payroll");
            Console.WriteLine("  4.  Holidays");
            Console.WriteLine("  5.  Update Personal Info");
            Console.WriteLine("  6.  Ask HR Bot");
            Console.WriteLine("  7.  Logout");
            Console.WriteLine("  8.  Exit");
        }
        else
        {
            Console.WriteLine("  1. Holidays");
            Console.WriteLine("  2. Check Own Salary");
            Console.WriteLine("  3. Update Personal Info");
            Console.WriteLine("  4. Submit Own Performance Review");
            Console.WriteLine("  5. Ask HR Bot");
            Console.WriteLine("  6. Logout");
            Console.WriteLine("  7. Exit");
        }

        Console.Write("\nChoice: ");
        var input = Console.ReadLine()?.Trim();

        var logoutOption = currentUser.Role switch
        {
            UserRole.HR => "8",
            UserRole.Manager => "7",
            UserRole.Employee => "6",
            _ => "1"
        };

        var exitOption = currentUser.Role switch
        {
            UserRole.HR => "9",
            UserRole.Manager => "8",
            UserRole.Employee => "7",
            _ => "1"
        };

        if (input == logoutOption)
        {
            Console.WriteLine($"\nLogged out. Goodbye, {currentUser.Name}!");
            loggedOut = true;
            continue;
        }

        if (input == exitOption)
        {
            Console.WriteLine("\nGoodbye!");
            return;
        }

    // --- HR: Employee Actions ---
    if (currentUser.Role == UserRole.HR && input == "1")
    {
        Console.WriteLine("\n  1. Add New Employee");
        Console.WriteLine("  2. Terminate Employee");
        Console.WriteLine("  3. Give Promotion");
        Console.WriteLine("  4. Check Pipeline Status");
        Console.Write("\nChoice: ");
        var subChoice = Console.ReadLine()?.Trim();
        if (subChoice == "1")
            await employeeManager.AddNewEmployeeAsync(currentUser);
        else if (subChoice == "2")
            await employeeManager.TerminateEmployeeAsync(currentUser);
        else if (subChoice == "3")
            await employeeManager.ReviewPromotionAsync(currentUser);
        else if (subChoice == "4")
            await employeeManager.CheckOnboardingStatusAsync();
        else
            Console.WriteLine("Invalid choice.");
    }
    // --- HR: Setup Salary Levels ---
    else if (currentUser.Role == UserRole.HR && input == "2")
    {
        await salaryLevelManager.SetupSalaryLevelsAsync();
    }
    // --- Holidays (HR=3, Manager=4, Employee=1) ---
    else if ((currentUser.Role == UserRole.HR && input == "3") ||
             (currentUser.Role == UserRole.Manager && input == "4") ||
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
    // --- Team Management (HR=4) ---
    else if (currentUser.Role == UserRole.HR && input == "4")
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
    // --- Performance Reviews (Manager=1) ---
    else if (currentUser.Role == UserRole.Manager && input == "1")
    {
        Console.WriteLine("\n  1. Review Team Performance");
        Console.WriteLine("  2. Submit Own Performance Review");
        Console.WriteLine("  3. Check Own History");
        Console.Write("\nChoice: ");
        var subChoice = Console.ReadLine()?.Trim();
        if (subChoice == "1")
            await performanceManager.ReviewTeamPerformanceAsync(currentUser);
        else if (subChoice == "2")
            await performanceManager.SubmitOwnReviewAsync(currentUser);
        else if (subChoice == "3")
            await performanceManager.CheckOwnHistoryAsync(currentUser);
        else
            Console.WriteLine("Invalid choice.");
    }
    // --- Propose Promotion (Manager=2) ---
    else if (currentUser.Role == UserRole.Manager && input == "2")
    {
        await employeeManager.ProposePromotionAsync(currentUser);
    }
    // --- Employee: Performance Review (4) ---
    else if (currentUser.Role == UserRole.Employee && input == "4")
    {
        Console.WriteLine("\n  1. Submit Performance Review");
        Console.WriteLine("  2. Check History");
        Console.Write("\nChoice: ");
        var subChoice = Console.ReadLine()?.Trim();
        if (subChoice == "1")
            await performanceManager.SubmitOwnReviewAsync(currentUser);
        else if (subChoice == "2")
            await performanceManager.CheckOwnHistoryAsync(currentUser);
        else
            Console.WriteLine("Invalid choice.");
    }
    // --- Payroll (HR=5, Manager=3) ---
    else if (currentUser.Role == UserRole.HR && input == "5")
    {
        await salaryLevelManager.CheckSomeonesSalaryAsync(currentUser);
    }
    else if (currentUser.Role == UserRole.Manager && input == "3")
    {
        Console.WriteLine("\n  1. Check Reportee Salary");
        Console.WriteLine("  2. Check Own Salary");
        Console.Write("\nChoice: ");
        var subChoice = Console.ReadLine()?.Trim();
        if (subChoice == "1")
            await salaryLevelManager.CheckSomeonesSalaryAsync(currentUser);
        else if (subChoice == "2")
            await salaryLevelManager.CheckOwnSalaryAsync(currentUser);
        else
            Console.WriteLine("Invalid choice.");
    }
    // --- Employee: Check Own Salary (2) ---
    else if (currentUser.Role == UserRole.Employee && input == "2")
    {
        await salaryLevelManager.CheckOwnSalaryAsync(currentUser);
    }
    // --- Update Personal Info (HR=6, Manager=5, Employee=3) ---
    else if ((currentUser.Role == UserRole.HR && input == "6") ||
             (currentUser.Role == UserRole.Manager && input == "5") ||
             (currentUser.Role == UserRole.Employee && input == "3"))
    {
        await employeeManager.UpdatePersonalInfoAsync(currentUser);
    }
    // --- Ask HR Bot (HR=7, Manager=6, Employee=5) ---
    else if ((currentUser.Role == UserRole.HR && input == "7") ||
             (currentUser.Role == UserRole.Manager && input == "6") ||
             (currentUser.Role == UserRole.Employee && input == "5"))
    {
        await aiManager.StartHRChatbotAsync();
    }
    else
    {
        Console.WriteLine("This feature is coming soon!");
    }
}
} // outer login loop
