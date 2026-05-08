using Microsoft.Azure.Cosmos;
using HRManagementService.Models;

namespace HRManagementService.Repository
{
    public class EmployeeRepository
    {
        private readonly Container _container;

        public EmployeeRepository(Container container)
        {
            _container = container;
        }

        public async Task CreateEmployeeAsync(Employee employee)
        {
            await _container.CreateItemAsync(employee, new PartitionKey(employee.Id));
        }

        public async Task<Employee?> GetByIdAsync(string employeeId)
        {
            try
            {
                var response = await _container.ReadItemAsync<Employee>(employeeId, new PartitionKey(employeeId));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task UpdateEmployeeAsync(Employee employee)
        {
            await _container.UpsertItemAsync(employee, new PartitionKey(employee.Id));
        }

        public async Task DeleteEmployeeAsync(string employeeId)
        {
            await _container.DeleteItemAsync<Employee>(employeeId, new PartitionKey(employeeId));
        }

        public async Task<Employee?> GetByAliasAsync(string alias)
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.alias = @alias")
                .WithParameter("@alias", alias);
            using var iterator = _container.GetItemQueryIterator<Employee>(query);
            if (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                return response.FirstOrDefault();
            }
            return null;
        }

        public async Task<List<Employee>> GetAllEmployeesAsync()
        {
            var query = new QueryDefinition("SELECT * FROM c");
            var employees = new List<Employee>();
            using var iterator = _container.GetItemQueryIterator<Employee>(query);
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                employees.AddRange(response);
            }
            return employees;
        }
    }
}
