namespace ContentForge.Infrastructure.Persistence.Entities;

public sealed class PermissionEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public ICollection<RolePermissionEntity> RolePermissions { get; set; } = [];
}
