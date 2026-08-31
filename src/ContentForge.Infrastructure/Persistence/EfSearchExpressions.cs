namespace ContentForge.Infrastructure.Persistence;

using ContentForge.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Provider-portable case-insensitive search predicates.
/// PostgreSQL uses ILike; SQL Server uses Like with CI collation defaults.
/// User input is escaped so % and _ are treated literally.
/// </summary>
internal static class EfSearchExpressions
{
    internal static IQueryable<ContentEntryEntity> WhereSlugContains(
        this IQueryable<ContentEntryEntity> query,
        AppDbContext dbContext,
        string pattern) =>
        dbContext.Database.IsNpgsql()
            ? query.Where(entry => EF.Functions.ILike(entry.Slug, pattern, PortableSearch.LikeEscapeCharacter))
            : query.Where(entry => EF.Functions.Like(entry.Slug, pattern, PortableSearch.LikeEscapeCharacter));

    internal static IQueryable<ContentTypeEntity> WhereContentTypeContains(
        this IQueryable<ContentTypeEntity> query,
        AppDbContext dbContext,
        string pattern) =>
        dbContext.Database.IsNpgsql()
            ? query.Where(contentType =>
                EF.Functions.ILike(contentType.Name, pattern, PortableSearch.LikeEscapeCharacter)
                || EF.Functions.ILike(contentType.DisplayName, pattern, PortableSearch.LikeEscapeCharacter)
                || EF.Functions.ILike(contentType.Slug, pattern, PortableSearch.LikeEscapeCharacter))
            : query.Where(contentType =>
                EF.Functions.Like(contentType.Name, pattern, PortableSearch.LikeEscapeCharacter)
                || EF.Functions.Like(contentType.DisplayName, pattern, PortableSearch.LikeEscapeCharacter)
                || EF.Functions.Like(contentType.Slug, pattern, PortableSearch.LikeEscapeCharacter));

    internal static IQueryable<ContentEntryEntity> WhereContentSearchContains(
        this IQueryable<ContentEntryEntity> query,
        AppDbContext dbContext,
        string pattern) =>
        dbContext.Database.IsNpgsql()
            ? query.Where(entry =>
                EF.Functions.ILike(entry.Slug, pattern, PortableSearch.LikeEscapeCharacter)
                || EF.Functions.ILike(entry.DraftDataJson, pattern, PortableSearch.LikeEscapeCharacter))
            : query.Where(entry =>
                EF.Functions.Like(entry.Slug, pattern, PortableSearch.LikeEscapeCharacter)
                || EF.Functions.Like(entry.DraftDataJson, pattern, PortableSearch.LikeEscapeCharacter));

    internal static IQueryable<MediaAssetEntity> WhereMediaContains(
        this IQueryable<MediaAssetEntity> query,
        AppDbContext dbContext,
        string pattern) =>
        dbContext.Database.IsNpgsql()
            ? query.Where(media =>
                EF.Functions.ILike(media.FileName, pattern, PortableSearch.LikeEscapeCharacter)
                || EF.Functions.ILike(media.OriginalFileName, pattern, PortableSearch.LikeEscapeCharacter)
                || EF.Functions.ILike(media.Title ?? string.Empty, pattern, PortableSearch.LikeEscapeCharacter))
            : query.Where(media =>
                EF.Functions.Like(media.FileName, pattern, PortableSearch.LikeEscapeCharacter)
                || EF.Functions.Like(media.OriginalFileName, pattern, PortableSearch.LikeEscapeCharacter)
                || EF.Functions.Like(media.Title ?? string.Empty, pattern, PortableSearch.LikeEscapeCharacter));

    internal static IQueryable<ContentForgeUser> WhereUserContains(
        this IQueryable<ContentForgeUser> query,
        AppDbContext dbContext,
        string pattern) =>
        dbContext.Database.IsNpgsql()
            ? query.Where(user =>
                EF.Functions.ILike(user.Email ?? string.Empty, pattern, PortableSearch.LikeEscapeCharacter)
                || EF.Functions.ILike(user.DisplayName, pattern, PortableSearch.LikeEscapeCharacter))
            : query.Where(user =>
                EF.Functions.Like(user.Email ?? string.Empty, pattern, PortableSearch.LikeEscapeCharacter)
                || EF.Functions.Like(user.DisplayName, pattern, PortableSearch.LikeEscapeCharacter));
}
