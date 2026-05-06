using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using HRManagementService.Repository;
using HRManagementService.AuthService;
using HRManagementService.Enums;

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var vaultUrl = config["KeyVault:VaultUrl"];
var databaseId = config["CosmosDb:DatabaseId"];
var cosmosSecretName = config["CosmosDb:ConnectionStringSecretName"];

if (string.IsNullOrEmpty(vaultUrl) || string.IsNullOrEmpty(databaseId) || string.IsNullOrEmpty(cosmosSecretName))
{
    Console.WriteLine("Error: Missing configuration values in appsettings.json.");
    return;
}

Console.WriteLine("Connecting to Azure Key Vault...");
var secretClient = new SecretClient(new Uri(vaultUrl), new DefaultAzureCredential());

string cosmosConnectionString;
try
{
    var cosmosSecret = await secretClient.GetSecretAsync(cosmosSecretName);
    cosmosConnectionString = cosmosSecret.Value.Value;
    Console.WriteLine("Secrets retrieved successfully.");
}
catch (Exception ex)
{
    Console.WriteLine($"Error fetching secrets: {ex.Message}");
    return;
}

Console.WriteLine("Initializing Cosmos DB...");
var cosmosService = await CosmosDbService.InitializeAsync(cosmosConnectionString, databaseId);

var usersContainerName = config["CosmosDb:Containers:Users"]!;
var usersContainer = await cosmosService.GetOrCreateContainerAsync(usersContainerName, "/id");
Console.WriteLine("Users container ready.");

var authRepo = new AuthRepository(usersContainer);
var authManager = new AuthManager(authRepo);

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
        Console.WriteLine("  1.  Add New Employee              [Coming Soon]");
        Console.WriteLine("  2.  Give Promotion / Raise        [Coming Soon]");
        Console.WriteLine("  3.  Fire Employee                 [Coming Soon]");
        Console.WriteLine("  4.  Setup Salary Levels           [Coming Soon]");
        Console.WriteLine("  5.  Setup Holiday Config          [Coming Soon]");
        Console.WriteLine("  6.  Create Team                   [Coming Soon]");
        Console.WriteLine("  7.  View Audit Logs               [Coming Soon]");
        Console.WriteLine("  8.  Check Employee Performance    [Coming Soon]");
        Console.WriteLine("  9.  Update Team Budget            [Coming Soon]");
        Console.WriteLine("  10. Check Someone's Salary        [Coming Soon]");
        Console.WriteLine("  11. View Team Info                [Coming Soon]");
        Console.WriteLine("  12. Check Holidays                [Coming Soon]");
        Console.WriteLine("  13. Check Own Salary              [Coming Soon]");
        Console.WriteLine("  14. Update Personal Info          [Coming Soon]");
        Console.WriteLine("  15. Request Holiday               [Coming Soon]");
        Console.WriteLine("  16. Submit Own Performance Review  [Coming Soon]");
        Console.WriteLine("  17. Check Own Holiday Bank         [Coming Soon]");
        Console.WriteLine("  18. Exit");
    }
    else if (currentUser.Role == UserRole.Manager)
    {
        Console.WriteLine("  1.  Check Employee Performance    [Coming Soon]");
        Console.WriteLine("  2.  Add Reportee Performance Review [Coming Soon]");
        Console.WriteLine("  3.  Flag Employee for Termination [Coming Soon]");
        Console.WriteLine("  4.  Update Team Budget            [Coming Soon]");
        Console.WriteLine("  5.  Check Someone's Salary        [Coming Soon]");
        Console.WriteLine("  6.  Check Readiness for Next Step [Coming Soon]");
        Console.WriteLine("  7.  View Team Info                [Coming Soon]");
        Console.WriteLine("  8.  Check Holidays                [Coming Soon]");
        Console.WriteLine("  9.  Check Own Salary              [Coming Soon]");
        Console.WriteLine("  10. Update Personal Info          [Coming Soon]");
        Console.WriteLine("  11. Request Holiday               [Coming Soon]");
        Console.WriteLine("  12. Submit Own Performance Review  [Coming Soon]");
        Console.WriteLine("  13. Check Own Holiday Bank         [Coming Soon]");
        Console.WriteLine("  14. Exit");
    }
    else
    {
        Console.WriteLine("  1. Check Holidays                [Coming Soon]");
        Console.WriteLine("  2. Check Own Salary              [Coming Soon]");
        Console.WriteLine("  3. Update Personal Info          [Coming Soon]");
        Console.WriteLine("  4. Request Holiday               [Coming Soon]");
        Console.WriteLine("  5. Submit Own Performance Review  [Coming Soon]");
        Console.WriteLine("  6. Check Own Holiday Bank         [Coming Soon]");
        Console.WriteLine("  7. Exit");
    }

    Console.Write("\nChoice: ");
    var input = Console.ReadLine()?.Trim();

    var exitOption = currentUser.Role switch
    {
        UserRole.HR => "18",
        UserRole.Manager => "14",
        UserRole.Employee => "7",
        _ => "1"
    };

    if (input == exitOption)
    {
        Console.WriteLine("\nGoodbye!");
        break;
    }

    Console.WriteLine("This feature is coming soon!");
}
