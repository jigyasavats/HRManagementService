using HRManagementService.Models;
using HRManagementService.Repository;
using Newtonsoft.Json;

namespace HRManagementService.Pipeline
{
    public class OffboardingPipeline
    {
        private readonly EmployeeRepository _employeeRepo;
        private readonly TeamRepository _teamRepo;
        private readonly PayrollRepository _payrollRepo;
        private readonly AuthRepository _authRepo;
        private readonly AuditRepository _auditRepo;
        private readonly OnboardingRepository _onboardingRepo;
        private readonly ServiceBusService _serviceBus;
        private readonly Dictionary<string, string> _queueNames;

        public OffboardingPipeline(
            EmployeeRepository employeeRepo,
            TeamRepository teamRepo,
            PayrollRepository payrollRepo,
            AuthRepository authRepo,
            AuditRepository auditRepo,
            OnboardingRepository onboardingRepo,
            ServiceBusService serviceBus,
            Dictionary<string, string> queueNames)
        {
            _employeeRepo = employeeRepo;
            _teamRepo = teamRepo;
            _payrollRepo = payrollRepo;
            _authRepo = authRepo;
            _auditRepo = auditRepo;
            _onboardingRepo = onboardingRepo;
            _serviceBus = serviceBus;
            _queueNames = queueNames;
        }

        public async Task<string?> StartAsync(OffboardingEvent offboardingData, string performedBy, string performedByRole)
        {
            Console.WriteLine("\n[Step 1/5] Marking employee as Terminated...");

            var employee = await _employeeRepo.GetByIdAsync(offboardingData.EmployeeId);
            if (employee == null)
            {
                Console.WriteLine("  Employee record not found. Aborting.");
                return null;
            }

            employee.Status = "Terminated";
            employee.TerminatedOn = DateTime.UtcNow;
            await _employeeRepo.UpdateEmployeeAsync(employee);
            Console.WriteLine($"  Employee '{employee.Name}' marked as Terminated.");

            var status = new OnboardingStatus
            {
                Id = Guid.NewGuid().ToString(),
                EmployeeId = offboardingData.EmployeeId,
                EmployeeName = offboardingData.Name,
                OverallStatus = "InProgress",
                Steps = new List<OnboardingStepStatus>
                {
                    new() { Name = "Mark Terminated", Status = "Done", CompletedAt = DateTime.UtcNow },
                    new() { Name = "Remove from Team", Status = "Pending" },
                    new() { Name = "Deactivate Auth", Status = "Pending" },
                    new() { Name = "Deactivate Payroll", Status = "Pending" },
                    new() { Name = "Audit Log", Status = "Pending" }
                }
            };

            await _onboardingRepo.CreateAsync(status);

            _ = Task.Run(() => RunBackgroundStepsAsync(status, offboardingData, performedBy, performedByRole));

            return status.Id;
        }

        private async Task RunBackgroundStepsAsync(OnboardingStatus status, OffboardingEvent data, string performedBy, string performedByRole)
        {
            try
            {
                var stepResults = new (bool success, string? error)[3];

                var parallelTasks = new List<Task>
                {
                    // Step 2: Remove from Team / Reassign if manager
                    Task.Run(async () => {
                        try
                        {
                            await _serviceBus.PublishAndProcessAsync<bool>(
                                _queueNames["TeamOperations"], data, async json =>
                                {
                                    var evt = JsonConvert.DeserializeObject<OffboardingEvent>(json)!;
                                    var allTeams = await _teamRepo.GetAllTeamsAsync();

                                    if (evt.IsManager)
                                    {
                                        // Find team where this person is manager
                                        var managedTeam = allTeams.FirstOrDefault(t => t.ManagerId == evt.Alias);
                                        if (managedTeam != null)
                                        {
                                            if (!string.IsNullOrEmpty(managedTeam.SkipManagerId))
                                            {
                                                // Reassign members to skip manager's team
                                                var skipTeam = allTeams.FirstOrDefault(t => t.ManagerId == managedTeam.SkipManagerId);
                                                if (skipTeam != null)
                                                {
                                                    foreach (var memberId in managedTeam.EmployeeIds)
                                                    {
                                                        if (!skipTeam.EmployeeIds.Contains(memberId))
                                                            skipTeam.EmployeeIds.Add(memberId);
                                                    }
                                                    await _teamRepo.UpdateTeamAsync(skipTeam);
                                                }
                                                managedTeam.EmployeeIds.Clear();
                                            }
                                            // Clear manager from team
                                            managedTeam.ManagerId = string.Empty;
                                            await _teamRepo.UpdateTeamAsync(managedTeam);
                                        }
                                    }
                                    else
                                    {
                                        // Remove from employeeIds in any team
                                        foreach (var team in allTeams)
                                        {
                                            if (team.EmployeeIds.Contains(evt.Alias))
                                            {
                                                team.EmployeeIds.Remove(evt.Alias);
                                                await _teamRepo.UpdateTeamAsync(team);
                                            }
                                        }
                                    }
                                    return true;
                                });
                            stepResults[0] = (true, null);
                        }
                        catch (Exception ex) { stepResults[0] = (false, ex.Message); }
                    }),

                    // Step 3: Deactivate Auth
                    Task.Run(async () => {
                        try
                        {
                            await _serviceBus.PublishAndProcessAsync<bool>(
                                _queueNames["EmployeeOffboarding"], data, async json =>
                                {
                                    var evt = JsonConvert.DeserializeObject<OffboardingEvent>(json)!;
                                    var authUser = await _authRepo.GetByAliasAsync(evt.Alias);
                                    if (authUser != null)
                                    {
                                        authUser.IsActive = false;
                                        await _authRepo.UpdateUserAsync(authUser);
                                    }
                                    return true;
                                });
                            stepResults[1] = (true, null);
                        }
                        catch (Exception ex) { stepResults[1] = (false, ex.Message); }
                    }),

                    // Step 4: Deactivate Payroll
                    Task.Run(async () => {
                        try
                        {
                            await _serviceBus.PublishAndProcessAsync<bool>(
                                _queueNames["PayrollOperations"], data, async json =>
                                {
                                    var evt = JsonConvert.DeserializeObject<OffboardingEvent>(json)!;
                                    var payroll = await _payrollRepo.GetPayrollByEmployeeIdAsync(evt.EmployeeId);
                                    if (payroll != null)
                                    {
                                        payroll.Status = "Inactive";
                                        payroll.LastUpdated = DateTime.UtcNow;
                                        await _payrollRepo.UpdatePayrollAsync(payroll);
                                    }
                                    return true;
                                });
                            stepResults[2] = (true, null);
                        }
                        catch (Exception ex) { stepResults[2] = (false, ex.Message); }
                    })
                };

                await Task.WhenAll(parallelTasks);

                for (int i = 0; i < 3; i++)
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

                // Step 5: Audit Log
                try
                {
                    var auditLog = new AuditLog
                    {
                        Action = "Employee Terminated",
                        PerformedBy = performedBy,
                        PerformedByRole = performedByRole,
                        TargetEmployeeId = data.EmployeeId,
                        Details = $"Terminated {data.Name} ({data.Alias}). Manager: {data.IsManager}"
                    };
                    await _auditRepo.LogAsync(auditLog);
                    status.Steps[4].Status = "Done";
                    status.Steps[4].CompletedAt = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    status.Steps[4].Status = "Failed";
                    status.Steps[4].ErrorMessage = ex.Message;
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
