namespace ContentForge.Infrastructure.Persistence;

using ContentForge.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Provider-portable case-insensitive search predicates.
/// PostgreSQL uses ILike; SQL Server uses Like with CI collation defaults.
/// </summary>
internal static class EfSearchExpressions
{
    internal static IQueryable<ContentEntryEntity> WhereSlugContains(
        this IQueryable<ContentEntryEntity> query,
        AppDbContext dbContext,
        string pattern) =>
        dbContext.Database.IsNpgsql()
            ? query.Where(entry => EF.Functions.ILike(entry.Slug, pattern))
            : query.Where(entry => EF.Functions.Like(entry.Slug, pattern));

    internal static IQueryable<ContentTypeEntity> WhereContentTypeContains(
        this IQueryable<ContentTypeEntity> query,
        AppDbContext dbContext,
        string pattern) =>
        dbContext.Database.IsNpgsql()
            ? query.Where(contentType =>
                EF.Functions.ILike(contentType.Name, pattern)
                || EF.Functions.ILike(contentType.DisplayName, pattern)
                || EF.Functions.ILike(contentType.Slug, pattern))
            : query.Where(contentType =>
                EF.Functions.Like(contentType.Name, pattern)
                || EF.Functions.Like(contentType.DisplayName, pattern)
                || EF.Functions.Like(contentType.Slug, pattern));

    internal static IQueryable<ContentEntryEntity> WhereContentSearchContains(
        this IQueryable<ContentEntryEntity> query,
        AppDbContext dbContext,
        string pattern) =>
        dbContext.Database.IsNpgsql()
            ? query.Where(entry =>
                EF.Functions.ILike(entry.Slug, pattern)
                || EF.Functions.ILike(entry.DraftDataJson, pattern))
            : query.Where(entry =>
                EF.Functions.Like(entry.Slug, pattern)
                || EF.Functions.Like(entry.DraftDataJson, pattern));

    internal static IQueryable<MediaAssetEntity> WhereMediaContains(
        this IQueryable<MediaAssetEntity> query,
        AppDbContext dbContext,
        string pattern) =>
        dbContext.Database.IsNpgsql()
            ? query.Where(media =>
                EF.Functions.ILike(media.FileName, pattern)
                || EF.Functions.ILike(media.OriginalFileName, pattern)
                || EF.Functions.ILike(media.Title ?? string.Empty, pattern))
            : query.Where(media =>
                EF.Functions.Like(media.FileName, pattern)
                || EF.Functions.Like(media.OriginalFileName, pattern)
                || EF.Functions.Like(media.Title ?? string.Empty, pattern));

    internal static IQueryable<ContentForgeUser> WhereUserContains(
        this IQueryable<ContentForgeUser> query,
        AppDbContext dbContext,
        string pattern) =>
        dbContext.Database.IsNpgsql()
            ? query.Where(user =>
                EF.Functions.ILike(user.Email ?? string.Empty, pattern)
                || EF.Functions.ILike(user.DisplayName, pattern))
            : query.Where(user =>
                EF.Functions.Like(user.Email ?? string.Empty, pattern)
                || EF.Functions.Like(user.DisplayName, pattern));
}
