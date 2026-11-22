namespace TaskFlow.Domain.Constants;

// Giá trị khớp ràng buộc CHECK trong schema.sql: workspace_members.role
public static class WorkspaceRoles
{
    public const string Owner = "owner";
    public const string Member = "member";
    public const string Viewer = "viewer";
}
