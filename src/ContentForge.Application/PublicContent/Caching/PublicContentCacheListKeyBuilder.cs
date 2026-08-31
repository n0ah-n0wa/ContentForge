namespace ContentForge.Application.PublicContent.Caching;

using ContentForge.Application.Content.Queries;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

internal static class PublicContentCacheListKeyBuilder
{
    internal static string Build(PublicContentListCriteria criteria)
    {
        var builder = new StringBuilder(256);
        builder.Append(criteria.Pagination.NormalizedPage);
        builder.Append('|');
        builder.Append(criteria.Pagination.NormalizedPageSize);
        builder.Append('|');
        builder.Append(criteria.Sort.SortBy);
        builder.Append('|');
        builder.Append(criteria.Sort.Direction);
        builder.Append('|');
        builder.Append(criteria.Slug);
        builder.Append('|');
        builder.Append(criteria.Search);
        builder.Append('|');
        builder.Append(criteria.PublishedFrom?.ToString("O", CultureInfo.InvariantCulture));
        builder.Append('|');
        builder.Append(criteria.PublishedTo?.ToString("O", CultureInfo.InvariantCulture));

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        return Convert.ToHexString(hash);
    }
}
