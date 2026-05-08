using Microsoft.Azure.Cosmos;
using HRManagementService.Models;

namespace HRManagementService.Repository
{
    public class TeamRepository
    {
        private readonly Container _container;

        public TeamRepository(Container container)
        {
            _container = container;
        }

        public async Task CreateTeamAsync(Team team)
        {
            await _container.CreateItemAsync(team, new PartitionKey(team.TeamId));
        }

        public async Task<Team?> GetByTeamIdAsync(string teamId)
        {
            try
            {
                var query = new QueryDefinition("SELECT * FROM c WHERE c.teamId = @teamId")
                    .WithParameter("@teamId", teamId);
                using var iterator = _container.GetItemQueryIterator<Team>(query);
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

        public async Task UpdateTeamAsync(Team team)
        {
            await _container.UpsertItemAsync(team, new PartitionKey(team.TeamId));
        }

        public async Task<List<Team>> GetAllTeamsAsync()
        {
            var query = new QueryDefinition("SELECT * FROM c");
            var teams = new List<Team>();
            using var iterator = _container.GetItemQueryIterator<Team>(query);
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                teams.AddRange(response);
            }
            return teams;
        }
    }
}
