using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;

namespace HRManagementService.Bootstrap;

public class AppSecrets
{
    public required string CosmosConnectionString { get; init; }
    public required string ServiceBusConnectionString { get; init; }
    public required string OpenAiApiKey { get; init; }
    public required string OpenAiEndpoint { get; init; }
}

public static class KeyVaultBootstrapper
{
    public static async Task<AppSecrets> LoadSecretsAsync(IConfiguration config)
    {
        var vaultUrl = config["KeyVault:VaultUrl"];
        if (string.IsNullOrEmpty(vaultUrl))
            throw new InvalidOperationException("KeyVault:VaultUrl is missing in configuration.");

        Console.WriteLine("Connecting to Azure Key Vault...");
        var secretClient = new SecretClient(new Uri(vaultUrl), new DefaultAzureCredential());

        var cosmosSecret = await secretClient.GetSecretAsync(config["CosmosDb:ConnectionStringSecretName"]);
        var serviceBusSecret = await secretClient.GetSecretAsync(config["ServiceBus:ConnectionStringSecretName"]);
        var openAiKeySecret = await secretClient.GetSecretAsync(config["OpenAI:ApiKeySecretName"]);
        var openAiEndpointSecret = await secretClient.GetSecretAsync(config["OpenAI:EndpointSecretName"]);

        Console.WriteLine("Secrets retrieved successfully.");

        return new AppSecrets
        {
            CosmosConnectionString = cosmosSecret.Value.Value,
            ServiceBusConnectionString = serviceBusSecret.Value.Value,
            OpenAiApiKey = openAiKeySecret.Value.Value,
            OpenAiEndpoint = openAiEndpointSecret.Value.Value
        };
    }
}
