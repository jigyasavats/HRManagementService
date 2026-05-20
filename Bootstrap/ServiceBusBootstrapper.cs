using HRManagementService.Pipeline;
using Microsoft.Extensions.Configuration;

namespace HRManagementService.Bootstrap;

public static class ServiceBusBootstrapper
{
    public static ServiceBusService Initialize(string connectionString)
    {
        return new ServiceBusService(connectionString);
    }

    public static Dictionary<string, string> LoadQueueNames(IConfiguration config)
    {
        return new Dictionary<string, string>
        {
            ["EmployeeOnboarding"] = config["ServiceBus:Queues:EmployeeOnboarding"]!,
            ["EmployeeOffboarding"] = config["ServiceBus:Queues:EmployeeOffboarding"]!,
            ["PromotionRaise"] = config["ServiceBus:Queues:PromotionRaise"]!,
            ["PayrollOperations"] = config["ServiceBus:Queues:PayrollOperations"]!,
            ["HolidayRequests"] = config["ServiceBus:Queues:HolidayRequests"]!,
            ["PerformanceReviews"] = config["ServiceBus:Queues:PerformanceReviews"]!,
            ["TeamOperations"] = config["ServiceBus:Queues:TeamOperations"]!
        };
    }
}
