using Microsoft.Azure.Cosmos;
using HRManagementService.Models;

namespace HRManagementService.Repository
{
    public class PerformanceRepository
    {
        private readonly Container _container;

        public PerformanceRepository(Container container)
        {
            _container = container;
        }

        public async Task CreatePerformanceRecordAsync(PerformanceReview review)
        {
            await _container.CreateItemAsync(review, new PartitionKey(review.Alias));
        }

        public async Task<PerformanceReview?> GetByAliasAsync(string alias)
        {
            try
            {
                var query = new QueryDefinition("SELECT * FROM c WHERE c.alias = @alias")
                    .WithParameter("@alias", alias);
                using var iterator = _container.GetItemQueryIterator<PerformanceReview>(query,
                    requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(alias) });
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

        public async Task UpdatePerformanceAsync(PerformanceReview review)
        {
            await _container.UpsertItemAsync(review, new PartitionKey(review.Alias));
        }

        public async Task<List<PerformanceReview>> GetAllAsync()
        {
            var query = new QueryDefinition("SELECT * FROM c");
            var reviews = new List<PerformanceReview>();
            using var iterator = _container.GetItemQueryIterator<PerformanceReview>(query);
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                reviews.AddRange(response);
            }
            return reviews;
        }
    }
}
