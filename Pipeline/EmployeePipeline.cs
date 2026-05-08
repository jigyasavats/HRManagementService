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
        private readonly PerformanceRepository _performanceRepo;
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
            PerformanceRepository performanceRepo,
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
            _performanceRepo = performanceRepo;
            _authRepo = authRepo;
            _auditRepo = auditRepo;
            _onboardingRepo = onboardingRepo;
            _serviceBus = serviceBus;
            _queueNames = queueNames;
        }

        public async Task<string?> StartAsync(OnboardingEvent onboardingData, string performedBy, string performedByRole)
        {
            // === STEP 1: Create Employee (Sequential — must succeed before anything else) ===
            Console.WriteLine("\n[Step 1/7] Creating employee record...");
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

            // === Save onboarding status tracker ===
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
                    new() { Name = "Create Performance Record", Status = "Pending" },
                    new() { Name = "Create Auth User", Status = "Pending" },
                    new() { Name = "Audit Log", Status = "Pending" }
                }
            };

            await _onboardingRepo.CreateAsync(status);

            // === STEPS 2-6: Fire in background via Event Hub ===
            _ = Task.Run(() => RunBackgroundStepsAsync(status, onboardingData, performedBy, performedByRole));

            return status.Id;
        }

        private async Task RunBackgroundStepsAsync(OnboardingStatus status, OnboardingEvent data, string performedBy, string performedByRole)
        {
            try
            {
                // Track parallel step results locally to avoid concurrent Cosmos writes
                var stepResults = new (bool success, string? error)[5];

                var parallelTasks = new List<Task>
                {
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
                                            // Remove from any other team first (one team rule)
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

                    Task.Run(async () => {
                        try
                        {
                            await _serviceBus.PublishAndProcessAsync<bool>(
                                _queueNames["PerformanceReviews"], data, async json =>
                                {
                                    var evt = JsonConvert.DeserializeObject<OnboardingEvent>(json)!;
                                    var review = new PerformanceReview
                                    {
                                        Id = Guid.NewGuid().ToString(),
                                        Alias = evt.Alias
                                    };
                                    await _performanceRepo.CreatePerformanceRecordAsync(review);
                                    return true;
                                });
                            stepResults[3] = (true, null);
                        }
                        catch (Exception ex) { stepResults[3] = (false, ex.Message); }
                    }),

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
                            stepResults[4] = (true, null);
                        }
                        catch (Exception ex) { stepResults[4] = (false, ex.Message); }
                    })
                };

                await Task.WhenAll(parallelTasks);

                // Update all step statuses in ONE write (no race condition)
                for (int i = 0; i < 5; i++)
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

                // === STEP 7: Audit Log (runs after all parallel steps) ===
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
                    status.Steps[6].Status = "Done";
                    status.Steps[6].CompletedAt = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    status.Steps[6].Status = "Failed";
                    status.Steps[6].ErrorMessage = ex.Message;
                }

                // Single final write with all results
                var allDone = status.Steps.All(s => s.Status == "Done");
                status.OverallStatus = allDone ? "Completed" : "CompletedWithErrors";
                status.CompletedAt = DateTime.UtcNow;
                await _onboardingRepo.UpdateAsync(status);
            }
            catch (Exception ex)
            {
                status.OverallStatus = "Failed";
                status.CompletedAt = DateTime.UtcNow;
                await _onboardingRepo.UpdateAsync(status);
            }
        }
    }
}
