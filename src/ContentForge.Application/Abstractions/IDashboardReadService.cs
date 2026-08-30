namespace ContentForge.Application.Abstractions;

using ContentForge.Application.Dashboard.Models;
using ContentForge.Domain.Common;

/// <summary>
/// Read-only dashboard aggregation port.
/// </summary>
public interface IDashboardReadService
{
    Task<DashboardContentStatisticsDto> GetContentStatisticsAsync(
        UserId? authorId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DashboardRecentContentItemDto>> GetRecentContentAsync(
        UserId? authorId,
        int limit,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DashboardRecentActivityItemDto>> GetRecentActivityAsync(
        int limit,
        CancellationToken cancellationToken = default);
}
