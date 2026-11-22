using System;
using System.Collections.Generic;

namespace TaskFlow.Domain.Entities;

public partial class Workspace
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public Guid? CreatedById { get; set; }

    public bool IsPersonal { get; set; }

    public bool ChatEnabled { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();

    public virtual ICollection<Board> Boards { get; set; } = new List<Board>();

    public virtual Chat? Chat { get; set; }

    public virtual User? CreatedBy { get; set; }

    public virtual ICollection<Invitation> Invitations { get; set; } = new List<Invitation>();

    public virtual ICollection<Label> Labels { get; set; } = new List<Label>();

    public virtual ICollection<WorkspaceMember> WorkspaceMembers { get; set; } = new List<WorkspaceMember>();
}
