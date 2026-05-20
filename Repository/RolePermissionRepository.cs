using Microsoft.Azure.Cosmos;
using HRManagementService.Models;

namespace HRManagementService.Repository;

public class RolePermissionRepository
{
    private readonly Container _container;

    public RolePermissionRepository(Container container)
    {
        _container = container;
    }

    public async Task<RolePermission?> GetByRoleAsync(string role)
    {
        try
        {
            var response = await _container.ReadItemAsync<RolePermission>(role, new PartitionKey(role));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<List<RolePermission>> GetAllAsync()
    {
        var query = new QueryDefinition("SELECT * FROM c");
        using var iterator = _container.GetItemQueryIterator<RolePermission>(query);

        var results = new List<RolePermission>();
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }
        return results;
    }

    public async Task UpsertAsync(RolePermission rolePermission)
    {
        await _container.UpsertItemAsync(rolePermission, new PartitionKey(rolePermission.Role));
    }
}
