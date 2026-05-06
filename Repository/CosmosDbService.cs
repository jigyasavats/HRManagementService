using Microsoft.Azure.Cosmos;

namespace HRManagementService.Repository
{
    public class CosmosDbService
    {
        private readonly CosmosClient _client;
        private readonly Database _database;

        private CosmosDbService(CosmosClient client, Database database)
        {
            _client = client;
            _database = database;
        }

        public static async Task<CosmosDbService> InitializeAsync(string connectionString, string databaseId)
        {
            var client = new CosmosClient(connectionString);
            var databaseResponse = await client.CreateDatabaseIfNotExistsAsync(databaseId);
            return new CosmosDbService(client, databaseResponse.Database);
        }

        public async Task<Container> GetOrCreateContainerAsync(string containerId, string partitionKeyPath)
        {
            await _database.CreateContainerIfNotExistsAsync(containerId, partitionKeyPath);
            return _database.GetContainer(containerId);
        }
    }
}
