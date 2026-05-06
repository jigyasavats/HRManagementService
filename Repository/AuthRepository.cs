using Microsoft.Azure.Cosmos;
using HRManagementService.Models;

namespace HRManagementService.Repository
{
    public class AuthRepository
    {
        private readonly Container _container;

        public AuthRepository(Container container)
        {
            _container = container;
        }

        public async Task<AuthUser?> GetByEmployeeIdAsync(string employeeId)
        {
            try
            {
                var response = await _container.ReadItemAsync<AuthUser>(employeeId, new PartitionKey(employeeId));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task CreateUserAsync(AuthUser user)
        {
            await _container.CreateItemAsync(user, new PartitionKey(user.EmployeeId));
        }

        public async Task<bool> AnyUserExistsAsync()
        {
            var query = new QueryDefinition("SELECT VALUE COUNT(1) FROM c");
            using var iterator = _container.GetItemQueryIterator<int>(query);
            if (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                return response.FirstOrDefault() > 0;
            }
            return false;
        }
    }
}
