namespace HRManagementService.Enums;

public enum Permission
{
    AddEmployee,
    TerminateEmployee,
    ReviewPromotion,
    CheckPipelineStatus,

    SetupSalaryLevels,
    CheckAnySalary,
    CheckOwnSalary,

    ViewHolidays,
    RequestHoliday,
    CheckOwnHolidayBank,
    SetupHolidayConfig,
    ApproveRejectHoliday,

    CreateTeam,
    UpdateTeam,
    ViewAllTeams,

    ReviewTeamPerformance,
    SubmitOwnReview,
    CheckOwnHistory,

    ProposePromotion,

    UpdatePersonalInfo,
    AskHRBot,
    ManageActiveSessions
}
