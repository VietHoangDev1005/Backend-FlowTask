using System;
using System.Collections.Generic;

namespace TaskFlow.Domain.Entities;

public partial class ActivityLog
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public Guid? BoardId { get; set; }

    public Guid? ActorId { get; set; }

    public string Action { get; set; } = null!;

    public string EntityType { get; set; } = null!;

    public Guid EntityId { get; set; }

    public string Metadata { get; set; } = null!;

    public DateTime OccurredAt { get; set; }

    public virtual User? Actor { get; set; }

    public virtual Workspace Workspace { get; set; } = null!;
}
