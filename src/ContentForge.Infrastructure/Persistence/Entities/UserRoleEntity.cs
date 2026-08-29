namespace ContentForge.Infrastructure.Persistence.Entities;

public sealed class UserRoleEntity
{
    public Guid UserId { get; set; }

    public Guid RoleId { get; set; }

    public UserEntity User { get; set; } = null!;

    public RoleEntity Role { get; set; } = null!;
}
