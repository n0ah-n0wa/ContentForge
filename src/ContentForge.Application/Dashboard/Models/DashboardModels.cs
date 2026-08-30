namespace ContentForge.Application.Dashboard.Models;

using ContentForge.Domain.Audit;
using ContentForge.Domain.Content;

/// <summary>
/// Aggregated content counts for the administrative dashboard.
/// </summary>
public sealed record DashboardContentStatisticsDto(
    long TotalContent,
    long DraftCount,
    long InReviewCount,
    long PublishedCount,
    long ArchivedCount);

/// <summary>
/// Lightweight recent content item for dashboard navigation.
/// </summary>
public sealed record DashboardRecentContentItemDto(
    Guid Id,
    Guid ContentTypeId,
    string ContentTypeSlug,
    string ContentTypeDisplayName,
    string Slug,
    ContentStatus Status,
    DateTimeOffset UpdatedAt);

/// <summary>
/// Lightweight recent audit activity item for the dashboard.
/// </summary>
public sealed record DashboardRecentActivityItemDto(
    Guid Id,
    DateTimeOffset Timestamp,
    AuditAction Action,
    string EntityType,
    string EntityId,
    Guid? UserId);

/// <summary>
/// Dashboard payload with permission-aware sections.
/// </summary>
public sealed record DashboardDto(
    DashboardContentStatisticsDto? ContentStatistics,
    IReadOnlyList<DashboardRecentContentItemDto> RecentContent,
    IReadOnlyList<DashboardRecentActivityItemDto> RecentActivity);
