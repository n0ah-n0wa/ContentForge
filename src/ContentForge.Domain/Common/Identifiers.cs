namespace ContentForge.Domain.Common;

/// <summary>
/// Strongly typed identifier base helpers.
/// </summary>
public interface IStronglyTypedId<TValue>
    where TValue : notnull
{
    TValue Value { get; }
}

public readonly record struct ContentTypeId(Guid Value) : IStronglyTypedId<Guid>
{
    public static ContentTypeId New() => new(Guid.NewGuid());

    public static ContentTypeId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainValidationException(nameof(value), "Content type identifier cannot be empty.");
        }

        return new ContentTypeId(value);
    }

    public override string ToString() => Value.ToString();
}

public readonly record struct ContentEntryId(Guid Value) : IStronglyTypedId<Guid>
{
    public static ContentEntryId New() => new(Guid.NewGuid());

    public static ContentEntryId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainValidationException(nameof(value), "Content entry identifier cannot be empty.");
        }

        return new ContentEntryId(value);
    }

    public override string ToString() => Value.ToString();
}

public readonly record struct ContentVersionId(Guid Value) : IStronglyTypedId<Guid>
{
    public static ContentVersionId New() => new(Guid.NewGuid());

    public static ContentVersionId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainValidationException(nameof(value), "Content version identifier cannot be empty.");
        }

        return new ContentVersionId(value);
    }

    public override string ToString() => Value.ToString();
}

public readonly record struct ContentTypeFieldId(Guid Value) : IStronglyTypedId<Guid>
{
    public static ContentTypeFieldId New() => new(Guid.NewGuid());

    public static ContentTypeFieldId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainValidationException(nameof(value), "Content type field identifier cannot be empty.");
        }

        return new ContentTypeFieldId(value);
    }

    public override string ToString() => Value.ToString();
}

public readonly record struct MediaId(Guid Value) : IStronglyTypedId<Guid>
{
    public static MediaId New() => new(Guid.NewGuid());

    public static MediaId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainValidationException(nameof(value), "Media identifier cannot be empty.");
        }

        return new MediaId(value);
    }

    public override string ToString() => Value.ToString();
}

public readonly record struct UserId(Guid Value) : IStronglyTypedId<Guid>
{
    public static UserId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainValidationException(nameof(value), "User identifier cannot be empty.");
        }

        return new UserId(value);
    }

    public override string ToString() => Value.ToString();
}

public readonly record struct RoleId(Guid Value) : IStronglyTypedId<Guid>
{
    public static RoleId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainValidationException(nameof(value), "Role identifier cannot be empty.");
        }

        return new RoleId(value);
    }

    public override string ToString() => Value.ToString();
}

public readonly record struct AuditLogId(Guid Value) : IStronglyTypedId<Guid>
{
    public static AuditLogId New() => new(Guid.NewGuid());

    public static AuditLogId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainValidationException(nameof(value), "Audit log identifier cannot be empty.");
        }

        return new AuditLogId(value);
    }

    public override string ToString() => Value.ToString();
}
