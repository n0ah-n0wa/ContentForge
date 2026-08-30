namespace ContentForge.Application.Dashboard.Queries;

using ContentForge.Application.Abstractions;
using ContentForge.Application.Common;
using ContentForge.Application.Dashboard.Models;
using ContentForge.Domain.Authorization;
using ContentForge.Domain.Common;

public sealed record GetDashboardQuery;

public sealed class GetDashboardQueryHandler
{
    private const int RecentContentLimit = 5;
    private const int RecentActivityLimit = 10;

    private readonly IDashboardReadService _dashboardReadService;
    private readonly ICurrentUserService _currentUser;

    public GetDashboardQueryHandler(IDashboardReadService dashboardReadService, ICurrentUserService currentUser)
    {
        _dashboardReadService = dashboardReadService;
        _currentUser = currentUser;
    }

    public async Task<DashboardDto> HandleAsync(GetDashboardQuery query, CancellationToken cancellationToken)
    {
        var (userId, role) = ApplicationGuard.RequireAuthenticatedUser(_currentUser);

        var canReadContent = role.HasPermission(Permissions.ContentRead);
        var canReadAudit = role.HasPermission(Permissions.AuditRead);
        UserId? authorFilter = AuthorizationRules.CanModifyOwnContentOnly(role.Name) ? userId : (UserId?)null;

        DashboardContentStatisticsDto? statistics = null;
        IReadOnlyList<DashboardRecentContentItemDto> recentContent = [];
        IReadOnlyList<DashboardRecentActivityItemDto> recentActivity = [];

        if (canReadContent)
        {
            statistics = await _dashboardReadService.GetContentStatisticsAsync(authorFilter, cancellationToken);
            recentContent = await _dashboardReadService.GetRecentContentAsync(
                authorFilter,
                RecentContentLimit,
                cancellationToken);
        }

        if (canReadAudit)
        {
            recentActivity = await _dashboardReadService.GetRecentActivityAsync(
                RecentActivityLimit,
                cancellationToken);
        }

        return new DashboardDto(statistics, recentContent, recentActivity);
    }
}
