using Microsoft.Azure.Cosmos;
using HRManagementService.Models;

namespace HRManagementService.Repository;

public class SessionRepository
{
    private readonly Container _container;

    public SessionRepository(Container container)
    {
        _container = container;
    }

    public async Task CreateSessionAsync(UserSession session)
    {
        await _container.CreateItemAsync(session, new PartitionKey(session.Alias));
    }

    public async Task<UserSession?> GetActiveSessionAsync(string alias)
    {
        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.alias = @alias AND c.isActive = true ORDER BY c.loginTime DESC OFFSET 0 LIMIT 1")
            .WithParameter("@alias", alias);

        using var iterator = _container.GetItemQueryIterator<UserSession>(query,
            requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(alias) });

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            var session = response.FirstOrDefault();
            if (session != null) return session;
        }
        return null;
    }

    public async Task DeactivateSessionAsync(UserSession session)
    {
        session.IsActive = false;
        await _container.ReplaceItemAsync(session, session.Id, new PartitionKey(session.Alias));
    }

    public async Task<List<UserSession>> GetAllActiveSessionsAsync()
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.isActive = true");
        using var iterator = _container.GetItemQueryIterator<UserSession>(query);

        var sessions = new List<UserSession>();
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            sessions.AddRange(response);
        }
        return sessions;
    }
}
