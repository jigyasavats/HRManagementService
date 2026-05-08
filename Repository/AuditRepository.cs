using Microsoft.Azure.Cosmos;
using HRManagementService.Models;

namespace HRManagementService.Repository
{
    public class AuditRepository
    {
        private readonly Container _container;

        public AuditRepository(Container container)
        {
            _container = container;
        }

        public async Task LogAsync(AuditLog log)
        {
            await _container.CreateItemAsync(log, new PartitionKey(log.PerformedBy));
        }

        public async Task<List<AuditLog>> GetByPerformerAsync(string performedBy)
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.performedBy = @performedBy ORDER BY c.timestamp DESC")
                .WithParameter("@performedBy", performedBy);
            var logs = new List<AuditLog>();
            using var iterator = _container.GetItemQueryIterator<AuditLog>(query);
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                logs.AddRange(response);
            }
            return logs;
        }

        public async Task<List<AuditLog>> GetByTargetAsync(string targetEmployeeId)
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.targetEmployeeId = @target ORDER BY c.timestamp DESC")
                .WithParameter("@target", targetEmployeeId);
            var logs = new List<AuditLog>();
            using var iterator = _container.GetItemQueryIterator<AuditLog>(query);
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                logs.AddRange(response);
            }
            return logs;
        }
    }
}
