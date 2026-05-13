using HRManagementService.Models;
using HRManagementService.Repository;
using Newtonsoft.Json;

namespace HRManagementService.Pipeline
{
    public class EmployeePipeline
    {
        private readonly EmployeeRepository _employeeRepo;
        private readonly TeamRepository _teamRepo;
        private readonly PayrollRepository _payrollRepo;
        private readonly HolidayRepository _holidayRepo;
        private readonly AuthRepository _authRepo;
        private readonly AuditRepository _auditRepo;
        private readonly OnboardingRepository _onboardingRepo;
        private readonly ServiceBusService _serviceBus;
        private readonly Dictionary<string, string> _queueNames;

        public EmployeePipeline(
            EmployeeRepository employeeRepo,
            TeamRepository teamRepo,
            PayrollRepository payrollRepo,
            HolidayRepository holidayRepo,
            AuthRepository authRepo,
            AuditRepository auditRepo,
            OnboardingRepository onboardingRepo,
            ServiceBusService serviceBus,
            Dictionary<string, string> queueNames)
        {
            _employeeRepo = employeeRepo;
            _teamRepo = teamRepo;
            _payrollRepo = payrollRepo;
            _holidayRepo = holidayRepo;
            _authRepo = authRepo;
            _auditRepo = auditRepo;
            _onboardingRepo = onboardingRepo;
            _serviceBus = serviceBus;
            _queueNames = queueNames;
        }

        public async Task<string?> StartAsync(OnboardingEvent onboardingData, string performedBy, string performedByRole)
        {
            Console.WriteLine("\n[Step 1/6] Creating employee record...");
            Employee employee;
            try
            {
                employee = new Employee
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = onboardingData.Name,
                    Email = onboardingData.Email,
                    Alias = onboardingData.Alias,
                    TeamId = onboardingData.TeamId,
                    JoiningDate = DateTime.UtcNow
                };

                await _employeeRepo.CreateEmployeeAsync(employee);
                onboardingData.EmployeeId = employee.Id;
                Console.WriteLine($"  Employee created: {employee.Name} ({employee.Id})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Failed to create employee: {ex.Message}");
                return null;
            }

            var status = new OnboardingStatus
            {
                Id = Guid.NewGuid().ToString(),
                EmployeeId = employee.Id,
                EmployeeName = employee.Name,
                Steps = new List<OnboardingStepStatus>
                {
                    new() { Name = "Create Employee", Status = "Done", CompletedAt = DateTime.UtcNow },
                    new() { Name = "Assign to Team", Status = "Pending" },
                    new() { Name = "Create Payroll", Status = "Pending" },
                    new() { Name = "Create Holiday Bank", Status = "Pending" },
                    new() { Name = "Create Auth User", Status = "Pending" },
                    new() { Name = "Audit Log", Status = "Pending" }
                }
            };

            await _onboardingRepo.CreateAsync(status);

            _ = Task.Run(() => RunBackgroundStepsAsync(status, onboardingData, performedBy, performedByRole));

            return status.Id;
        }

        private async Task RunBackgroundStepsAsync(OnboardingStatus status, OnboardingEvent data, string performedBy, string performedByRole)
        {
            try
            {
                var stepResults = new (bool success, string? error)[4];

                var parallelTasks = new List<Task>
                {
                    // Step 2: Assign to Team
                    Task.Run(async () => {
                        try
                        {
                            await _serviceBus.PublishAndProcessAsync<bool>(
                                _queueNames["TeamOperations"], data, async json =>
                                {
                                    var evt = JsonConvert.DeserializeObject<OnboardingEvent>(json)!;
                                    var team = await _teamRepo.GetByTeamIdAsync(evt.TeamId);
                                    if (team != null)
                                    {
                                        if (evt.Role == Enums.UserRole.Manager)
                                        {
                                            team.ManagerId = evt.Alias;
                                        }
                                        else
                                        {
                                            var allTeams = await _teamRepo.GetAllTeamsAsync();
                                            foreach (var other in allTeams)
                                            {
                                                if (other.TeamId != team.TeamId && other.EmployeeIds.Contains(evt.Alias))
                                                {
                                                    other.EmployeeIds.Remove(evt.Alias);
                                                    await _teamRepo.UpdateTeamAsync(other);
                                                }
                                            }

                                            if (!team.EmployeeIds.Contains(evt.Alias))
                                                team.EmployeeIds.Add(evt.Alias);
                                        }
                                        await _teamRepo.UpdateTeamAsync(team);
                                    }
                                    return true;
                                });
                            stepResults[0] = (true, null);
                        }
                        catch (Exception ex) { stepResults[0] = (false, ex.Message); }
                    }),

                    // Step 3: Create Payroll
                    Task.Run(async () => {
                        try
                        {
                            await _serviceBus.PublishAndProcessAsync<bool>(
                                _queueNames["PayrollOperations"], data, async json =>
                                {
                                    var evt = JsonConvert.DeserializeObject<OnboardingEvent>(json)!;
                                    var payroll = new EmployeePayroll
                                    {
                                        Id = Guid.NewGuid().ToString(),
                                        EmployeeId = evt.EmployeeId,
                                        Alias = evt.Alias,
                                        Level = evt.Level,
                                        Salary = evt.Salary,
                                        LastUpdated = DateTime.UtcNow
                                    };
                                    await _payrollRepo.CreatePayrollAsync(payroll);
                                    return true;
                                });
                            stepResults[1] = (true, null);
                        }
                        catch (Exception ex) { stepResults[1] = (false, ex.Message); }
                    }),

                    // Step 4: Create Holiday Bank
                    Task.Run(async () => {
                        try
                        {
                            await _serviceBus.PublishAndProcessAsync<bool>(
                                _queueNames["HolidayRequests"], data, async json =>
                                {
                                    var evt = JsonConvert.DeserializeObject<OnboardingEvent>(json)!;
                                    var bank = new EmployeeHolidayBank
                                    {
                                        Id = Guid.NewGuid().ToString(),
                                        EmployeeId = evt.EmployeeId,
                                        AnnualLeaveBalance = evt.AnnualLeaveCount
                                    };
                                    await _holidayRepo.CreateHolidayBankAsync(bank);
                                    return true;
                                });
                            stepResults[2] = (true, null);
                        }
                        catch (Exception ex) { stepResults[2] = (false, ex.Message); }
                    }),

                    // Step 5: Create Auth User
                    Task.Run(async () => {
                        try
                        {
                            await _serviceBus.PublishAndProcessAsync<bool>(
                                _queueNames["EmployeeOnboarding"], data, async json =>
                                {
                                    var evt = JsonConvert.DeserializeObject<OnboardingEvent>(json)!;
                                    var authUser = new AuthUser
                                    {
                                        Id = Guid.NewGuid().ToString(),
                                        EmployeeId = evt.EmployeeId,
                                        Name = evt.Name,
                                        Email = evt.Email,
                                        Alias = evt.Alias,
                                        Role = evt.Role
                                    };
                                    await _authRepo.CreateUserAsync(authUser);
                                    return true;
                                });
                            stepResults[3] = (true, null);
                        }
                        catch (Exception ex) { stepResults[3] = (false, ex.Message); }
                    })
                };

                await Task.WhenAll(parallelTasks);

                for (int i = 0; i < 4; i++)
                {
                    if (stepResults[i].success)
                    {
                        status.Steps[i + 1].Status = "Done";
                        status.Steps[i + 1].CompletedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        status.Steps[i + 1].Status = "Failed";
                        status.Steps[i + 1].ErrorMessage = stepResults[i].error;
                    }
                }

                // Step 6: Audit Log
                try
                {
                    var auditLog = new AuditLog
                    {
                        Action = "Employee Added",
                        PerformedBy = performedBy,
                        PerformedByRole = performedByRole,
                        TargetEmployeeId = data.EmployeeId,
                        Details = $"Onboarded {data.Name} ({data.Email}) to team {data.TeamId} at level {data.Level}"
                    };
                    await _auditRepo.LogAsync(auditLog);
                    status.Steps[5].Status = "Done";
                    status.Steps[5].CompletedAt = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    status.Steps[5].Status = "Failed";
                    status.Steps[5].ErrorMessage = ex.Message;
                }

                var allDone = status.Steps.All(s => s.Status == "Done");
                status.OverallStatus = allDone ? "Completed" : "CompletedWithErrors";
                status.CompletedAt = DateTime.UtcNow;
                await _onboardingRepo.UpdateAsync(status);
            }
            catch (Exception)
            {
                status.OverallStatus = "Failed";
                status.CompletedAt = DateTime.UtcNow;
                await _onboardingRepo.UpdateAsync(status);
            }
        }
    }
}