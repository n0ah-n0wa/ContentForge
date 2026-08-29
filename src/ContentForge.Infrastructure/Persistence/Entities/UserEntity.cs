namespace ContentForge.Infrastructure.Persistence.Entities;

public sealed class UserEntity
{
    public Guid Id { get; set; }

    public string Email { get; set; } = null!;

    public string DisplayName { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset? LastLoginAt { get; set; }

    public ICollection<UserRoleEntity> UserRoles { get; set; } = [];
}
