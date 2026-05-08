using Microsoft.Azure.Cosmos;
using HRManagementService.Models;

namespace HRManagementService.Repository
{
    public class PayrollRepository
    {
        private readonly Container _levelContainer;
        private readonly Container _payrollContainer;

        public PayrollRepository(Container levelContainer, Container payrollContainer)
        {
            _levelContainer = levelContainer;
            _payrollContainer = payrollContainer;
        }

        public async Task CreateLevelAsync(LevelSalaryRange level)
        {
            await _levelContainer.CreateItemAsync(level, new PartitionKey(level.Level));
        }

        public async Task<LevelSalaryRange?> GetLevelAsync(string level)
        {
            try
            {
                var query = new QueryDefinition("SELECT * FROM c WHERE c.level = @level")
                    .WithParameter("@level", level);
                using var iterator = _levelContainer.GetItemQueryIterator<LevelSalaryRange>(query);
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

        public async Task<List<LevelSalaryRange>> GetAllLevelsAsync()
        {
            var query = new QueryDefinition("SELECT * FROM c");
            var levels = new List<LevelSalaryRange>();
            using var iterator = _levelContainer.GetItemQueryIterator<LevelSalaryRange>(query);
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                levels.AddRange(response);
            }
            return levels;
        }

        public async Task CreatePayrollAsync(EmployeePayroll payroll)
        {
            await _payrollContainer.CreateItemAsync(payroll, new PartitionKey(payroll.EmployeeId));
        }

        public async Task<EmployeePayroll?> GetPayrollByEmployeeIdAsync(string employeeId)
        {
            try
            {
                var query = new QueryDefinition("SELECT * FROM c WHERE c.employeeId = @employeeId")
                    .WithParameter("@employeeId", employeeId);
                using var iterator = _payrollContainer.GetItemQueryIterator<EmployeePayroll>(query);
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

        public async Task UpdatePayrollAsync(EmployeePayroll payroll)
        {
            await _payrollContainer.UpsertItemAsync(payroll, new PartitionKey(payroll.EmployeeId));
        }
    }
}
