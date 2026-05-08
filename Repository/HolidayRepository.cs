using Microsoft.Azure.Cosmos;
using HRManagementService.Models;

namespace HRManagementService.Repository
{
    public class HolidayRepository
    {
        private readonly Container _configContainer;
        private readonly Container _bankContainer;

        public HolidayRepository(Container configContainer, Container bankContainer)
        {
            _configContainer = configContainer;
            _bankContainer = bankContainer;
        }

        public async Task<HolidayConfig?> GetConfigAsync()
        {
            try
            {
                var response = await _configContainer.ReadItemAsync<HolidayConfig>("holiday-config", new PartitionKey("holiday-config"));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task UpsertConfigAsync(HolidayConfig config)
        {
            await _configContainer.UpsertItemAsync(config, new PartitionKey(config.Id));
        }

        public async Task CreateHolidayBankAsync(EmployeeHolidayBank bank)
        {
            await _bankContainer.CreateItemAsync(bank, new PartitionKey(bank.EmployeeId));
        }

        public async Task<EmployeeHolidayBank?> GetHolidayBankByEmployeeIdAsync(string employeeId)
        {
            try
            {
                var query = new QueryDefinition("SELECT * FROM c WHERE c.employeeId = @employeeId")
                    .WithParameter("@employeeId", employeeId);
                using var iterator = _bankContainer.GetItemQueryIterator<EmployeeHolidayBank>(query);
                if (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    return response.FirstOrDefault();
                }
                return null;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task UpdateHolidayBankAsync(EmployeeHolidayBank bank)
        {
            await _bankContainer.UpsertItemAsync(bank, new PartitionKey(bank.EmployeeId));
        }

        public async Task<List<EmployeeHolidayBank>> GetAllHolidayBanksAsync()
        {
            var query = new QueryDefinition("SELECT * FROM c");
            var banks = new List<EmployeeHolidayBank>();
            using var iterator = _bankContainer.GetItemQueryIterator<EmployeeHolidayBank>(query);
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                banks.AddRange(response);
            }
            return banks;
        }
    }
}
