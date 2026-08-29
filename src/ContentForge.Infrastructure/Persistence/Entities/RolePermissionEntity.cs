namespace ContentForge.Infrastructure.Persistence.Entities;

public sealed class RolePermissionEntity
{
    public Guid RoleId { get; set; }

    public Guid PermissionId { get; set; }

    public RoleEntity Role { get; set; } = null!;

    public PermissionEntity Permission { get; set; } = null!;
}
