namespace DesignGuard.Data.Entities;

public sealed class UserRoleEntity
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public ProjectEntity? Project { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
}
