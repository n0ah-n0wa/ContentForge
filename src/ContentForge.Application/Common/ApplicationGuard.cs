namespace ContentForge.Application.Common;

using ContentForge.Application.Abstractions;
using ContentForge.Application.Common.Concurrency;
using ContentForge.Application.Common.Exceptions;
using ContentForge.Domain.Authorization;
using ContentForge.Domain.Common;
using ContentForge.Domain.Content;

/// <summary>
/// Centralized authorization and domain exception translation for application services.
/// </summary>
public static class ApplicationGuard
{
    public static void EnsureAuthenticated(ICurrentUserService currentUser)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null || currentUser.Role is null)
        {
            throw new UnauthorizedApplicationException();
        }
    }

    public static (UserId UserId, RoleDefinition Role) RequireAuthenticatedUser(ICurrentUserService currentUser)
    {
        EnsureAuthenticated(currentUser);
        var userId = currentUser.UserId ?? throw new UnauthorizedApplicationException();
        var role = currentUser.Role ?? throw new UnauthorizedApplicationException();
        return (userId, role);
    }

    public static void EnsureCanIncludeDeleted(RoleDefinition role, PermissionName deletePermission, bool includeDeleted)
    {
        if (includeDeleted)
        {
            EnsurePermission(role, deletePermission);
        }
    }

    public static void EnsurePermission(RoleDefinition role, PermissionName permission)
    {
        try
        {
            AuthorizationRules.EnsureAllowed(role, permission);
        }
        catch (InvalidOperationDomainException exception)
        {
            throw new ForbiddenApplicationException(exception.Message);
        }
    }

    public static void EnsureCanModifyContent(RoleDefinition role, UserId actorId, UserId contentOwnerId)
    {
        try
        {
            AuthorizationRules.EnsureCanModifyContent(role, actorId, contentOwnerId);
        }
        catch (InvalidOperationDomainException exception)
        {
            throw new ForbiddenApplicationException(exception.Message);
        }
    }

    public static void EnsureCanReadContent(RoleDefinition role, UserId actorId, UserId contentOwnerId)
    {
        if (AuthorizationRules.CanModifyOwnContentOnly(role.Name) && actorId != contentOwnerId)
        {
            throw new ForbiddenApplicationException("Authors may only access content they created.");
        }
    }

    public static ContentEntry RequireVisibleContentEntry(ContentEntry? entry, Guid contentEntryId)
    {
        if (entry is null || entry.IsDeleted)
        {
            throw new NotFoundApplicationException("ContentEntry", contentEntryId);
        }

        return entry;
    }

    public static Slug CreateSlug(string value) =>
        TranslateDomainException(() => Slug.Create(value));

    public static FieldName CreateFieldName(string value) =>
        TranslateDomainException(() => FieldName.Create(value));

    public static ConcurrencyToken ToDomainToken(ConcurrencyRequest request) =>
        new((uint)request.Version);

    public static void TranslateDomainException(Action action)
    {
        try
        {
            action();
        }
        catch (ConcurrencyConflictException exception)
        {
            throw ToConcurrencyConflict(exception);
        }
        catch (DomainValidationException exception)
        {
            throw new ApplicationValidationException(exception.Field ?? string.Empty, exception.Message);
        }
        catch (InvalidOperationDomainException exception)
        {
            throw new ApplicationValidationException(string.Empty, exception.Message);
        }
    }

    public static T TranslateDomainException<T>(Func<T> action)
    {
        try
        {
            return action();
        }
        catch (ConcurrencyConflictException exception)
        {
            throw ToConcurrencyConflict(exception);
        }
        catch (DomainValidationException exception)
        {
            throw new ApplicationValidationException(exception.Field ?? string.Empty, exception.Message);
        }
        catch (InvalidOperationDomainException exception)
        {
            throw new ApplicationValidationException(string.Empty, exception.Message);
        }
    }

    public static async Task SaveChangesAsync(IUnitOfWork unitOfWork, CancellationToken cancellationToken)
    {
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (ConcurrencyConflictException exception)
        {
            throw ToConcurrencyConflict(exception);
        }
    }

    public static ConcurrencyConflictApplicationException ToConcurrencyConflict(ConcurrencyConflictException exception) =>
        new(new ConcurrencyConflictResult(exception.ExpectedVersion, exception.ActualVersion, DateTimeOffset.UtcNow));
}
