using Microsoft.Azure.Cosmos;
using HRManagementService.Models;

namespace HRManagementService.Repository
{
    public class PromotionRepository
    {
        private readonly Container _container;

        public PromotionRepository(Container container)
        {
            _container = container;
        }

        public async Task CreateAsync(PromotionRequest request)
        {
            await _container.CreateItemAsync(request, new PartitionKey(request.Alias));
        }

        public async Task UpdateAsync(PromotionRequest request)
        {
            await _container.UpsertItemAsync(request, new PartitionKey(request.Alias));
        }

        public async Task<PromotionRequest?> GetPendingByAliasAsync(string alias)
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.alias = @alias AND c.status = 'Pending'")
                .WithParameter("@alias", alias);
            using var iterator = _container.GetItemQueryIterator<PromotionRequest>(query,
                requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(alias) });
            if (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                return response.FirstOrDefault();
            }
            return null;
        }

        public async Task<List<PromotionRequest>> GetByAliasAsync(string alias)
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.alias = @alias")
                .WithParameter("@alias", alias);
            var results = new List<PromotionRequest>();
            using var iterator = _container.GetItemQueryIterator<PromotionRequest>(query,
                requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(alias) });
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }
            return results;
        }

        public async Task<List<PromotionRequest>> GetAllPendingAsync()
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.status = 'Pending'");
            var results = new List<PromotionRequest>();
            using var iterator = _container.GetItemQueryIterator<PromotionRequest>(query);
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }
            return results;
        }

        public async Task<List<PromotionRequest>> GetAllAsync()
        {
            var query = new QueryDefinition("SELECT * FROM c");
            var results = new List<PromotionRequest>();
            using var iterator = _container.GetItemQueryIterator<PromotionRequest>(query);
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }
            return results;
        }
    }
}
