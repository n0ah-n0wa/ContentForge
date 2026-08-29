namespace ContentForge.Application.Audit.Queries;

using ContentForge.Application.Abstractions.Persistence;
using ContentForge.Application.Audit.Models;
using ContentForge.Application.Common;
using ContentForge.Application.Common.Exceptions;
using ContentForge.Application.Common.Filtering;
using ContentForge.Application.Common.Pagination;
using ContentForge.Application.Mapping;
using ContentForge.Domain.Authorization;
using ContentForge.Domain.Common;

public sealed record GetAuditLogQuery(Guid AuditLogId);

public sealed class GetAuditLogQueryHandler
{
    private readonly IAuditLogRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public GetAuditLogQueryHandler(IAuditLogRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<AuditLogEntryDto> HandleAsync(GetAuditLogQuery query, CancellationToken cancellationToken)
    {
        var (_, role) = ApplicationGuard.RequireAuthenticatedUser(_currentUser);
        ApplicationGuard.EnsurePermission(role, Permissions.AuditRead);

        var entry = await _repository.GetByIdAsync(AuditLogId.From(query.AuditLogId), cancellationToken)
            ?? throw new NotFoundApplicationException("AuditLogEntry", query.AuditLogId);

        return AuditMapper.ToDto(entry);
    }
}

public sealed record ListAuditLogsQuery(AuditLogListCriteria Criteria);

public sealed class ListAuditLogsQueryHandler
{
    private readonly IAuditLogRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public ListAuditLogsQueryHandler(IAuditLogRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<PaginatedResult<AuditLogEntryDto>> HandleAsync(ListAuditLogsQuery query, CancellationToken cancellationToken)
    {
        var (_, role) = ApplicationGuard.RequireAuthenticatedUser(_currentUser);
        ApplicationGuard.EnsurePermission(role, Permissions.AuditRead);

        query.Criteria.Sort.EnsureAllowed(AuditLogListCriteria.AllowedSortFields, "audit logs");
        ValidateFilters(query.Criteria);

        var result = await _repository.ListAsync(query.Criteria, cancellationToken);
        return new PaginatedResult<AuditLogEntryDto>(
            result.Items.Select(AuditMapper.ToDto).ToList(),
            result.Page,
            result.PageSize,
            result.TotalItems);
    }

    private static void ValidateFilters(AuditLogListCriteria criteria)
    {
        var supplied = new Dictionary<string, string?>(StringComparer.Ordinal);
        if (criteria.UserId is not null)
        {
            supplied["userId"] = criteria.UserId.Value.Value.ToString();
        }

        if (criteria.Action is not null)
        {
            supplied["action"] = criteria.Action.Value.ToString();
        }

        if (!string.IsNullOrWhiteSpace(criteria.EntityType))
        {
            supplied["entityType"] = criteria.EntityType;
        }

        if (!string.IsNullOrWhiteSpace(criteria.EntityId))
        {
            supplied["entityId"] = criteria.EntityId;
        }

        if (criteria.From is not null)
        {
            supplied["from"] = criteria.From.Value.ToString("O");
        }

        if (criteria.To is not null)
        {
            supplied["to"] = criteria.To.Value.ToString("O");
        }

        FilterValidator.EnsureAllowed(supplied, AuditLogListCriteria.AllowedFilterFields, "audit logs");
    }
}
