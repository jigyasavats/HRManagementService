using Microsoft.Azure.Cosmos;
using HRManagementService.Models;
using HRManagementService.Enums;

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
            var query = new QueryDefinition("SELECT * FROM c WHERE c.employeeId = @eid")
                .WithParameter("@eid", employeeId);
            using var iterator = _container.GetItemQueryIterator<AuthUser>(query);
            if (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                return response.FirstOrDefault();
            }
            return null;
        }

        public async Task<AuthUser?> GetByAliasAsync(string alias)
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.alias = @alias")
                .WithParameter("@alias", alias);
            using var iterator = _container.GetItemQueryIterator<AuthUser>(query);
            if (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                return response.FirstOrDefault();
            }
            return null;
        }

        public async Task CreateUserAsync(AuthUser user)
        {
            await _container.CreateItemAsync(user, new PartitionKey(user.Email));
        }

        public async Task UpdateUserAsync(AuthUser user)
        {
            await _container.UpsertItemAsync(user, new PartitionKey(user.Email));
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

        public async Task<List<AuthUser>> GetByRoleAsync(UserRole role)
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.role = @role")
                .WithParameter("@role", (int)role);
            var users = new List<AuthUser>();
            using var iterator = _container.GetItemQueryIterator<AuthUser>(query);
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                users.AddRange(response);
            }
            return users;
        }

        public async Task<List<AuthUser>> GetAllUsersAsync()
        {
            var query = new QueryDefinition("SELECT * FROM c");
            var users = new List<AuthUser>();
            using var iterator = _container.GetItemQueryIterator<AuthUser>(query);
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                users.AddRange(response);
            }
            return users;
        }
    }
}
