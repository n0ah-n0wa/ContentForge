namespace ContentForge.Infrastructure.Persistence.Entities;

using Microsoft.AspNetCore.Identity;

public sealed class ContentForgeUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = null!;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset? LastLoginAt { get; set; }

    public ICollection<UserRoleEntity> UserRoles { get; set; } = [];

    public ICollection<RefreshTokenEntity> RefreshTokens { get; set; } = [];
}
