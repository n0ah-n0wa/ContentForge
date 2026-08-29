namespace ContentForge.Domain.Content;

/// <summary>
/// Describes a field-level difference between two content snapshots.
/// </summary>
public sealed record ContentFieldChange(
    string FieldName,
    object? OldValue,
    object? NewValue);

/// <summary>
/// Compares content snapshots and versions.
/// </summary>
public static class ContentVersionComparer
{
    public static IReadOnlyList<ContentFieldChange> Compare(ContentSnapshot left, ContentSnapshot right)
    {
        var fieldNames = left.Data.Values.Keys
            .Union(right.Data.Values.Keys, StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal);

        var changes = new List<ContentFieldChange>();
        foreach (var fieldName in fieldNames)
        {
            var oldValue = left.Data.GetValue(fieldName);
            var newValue = right.Data.GetValue(fieldName);

            if (!Equals(oldValue, newValue))
            {
                changes.Add(new ContentFieldChange(fieldName, oldValue, newValue));
            }
        }

        if (!left.Slug.Equals(right.Slug))
        {
            changes.Insert(0, new ContentFieldChange("_slug", left.Slug.Value, right.Slug.Value));
        }

        if (left.Status != right.Status)
        {
            changes.Insert(0, new ContentFieldChange("_status", left.Status, right.Status));
        }

        return changes;
    }

    public static IReadOnlyList<ContentFieldChange> Compare(ContentVersion left, ContentVersion right) =>
        Compare(left.Snapshot, right.Snapshot);
}
