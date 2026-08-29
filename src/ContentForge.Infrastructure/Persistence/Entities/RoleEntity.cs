namespace ContentForge.Infrastructure.Persistence.Entities;

public sealed class RoleEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public ICollection<UserRoleEntity> UserRoles { get; set; } = [];

    public ICollection<RolePermissionEntity> RolePermissions { get; set; } = [];
}
