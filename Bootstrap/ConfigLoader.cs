using Microsoft.Extensions.Configuration;

namespace HRManagementService.Bootstrap;

public static class ConfigLoader
{
    public static IConfiguration Load()
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();
    }
}
