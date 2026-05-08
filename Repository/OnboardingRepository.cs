using Microsoft.Azure.Cosmos;
using HRManagementService.Models;

namespace HRManagementService.Repository
{
    public class OnboardingRepository
    {
        private readonly Container _container;

        public OnboardingRepository(Container container)
        {
            _container = container;
        }

        public async Task CreateAsync(OnboardingStatus status)
        {
            await _container.CreateItemAsync(status, new PartitionKey(status.Id));
        }

        public async Task UpdateAsync(OnboardingStatus status)
        {
            await _container.UpsertItemAsync(status, new PartitionKey(status.Id));
        }

        public async Task<List<OnboardingStatus>> GetInProgressAsync()
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.overallStatus = 'InProgress' ORDER BY c.startedAt DESC");
            var results = new List<OnboardingStatus>();
            using var iterator = _container.GetItemQueryIterator<OnboardingStatus>(query);
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }
            return results;
        }

        public async Task<List<OnboardingStatus>> GetAllAsync()
        {
            var query = new QueryDefinition("SELECT * FROM c ORDER BY c.startedAt DESC");
            var results = new List<OnboardingStatus>();
            using var iterator = _container.GetItemQueryIterator<OnboardingStatus>(query);
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }
            return results;
        }
    }
}
