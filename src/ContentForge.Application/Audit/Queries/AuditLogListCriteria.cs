namespace ContentForge.Application.Audit.Queries;

using ContentForge.Application.Common.Pagination;
using ContentForge.Application.Common.Sorting;
using ContentForge.Domain.Audit;
using ContentForge.Domain.Common;

/// <summary>
/// Query criteria for listing audit log entries.
/// </summary>
public sealed record AuditLogListCriteria(
    PaginationRequest Pagination,
    SortRequest Sort,
    UserId? UserId = null,
    AuditAction? Action = null,
    string? EntityType = null,
    string? EntityId = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null)
{
    public static IReadOnlySet<string> AllowedSortFields { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "timestamp",
        "action",
        "entityType",
    };

    public static IReadOnlySet<string> AllowedFilterFields { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "userId",
        "action",
        "entityType",
        "entityId",
        "from",
        "to",
    };
}
