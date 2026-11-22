using System;
using System.Collections.Generic;

namespace TaskFlow.Domain.Entities;

public partial class Invitation
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public string Email { get; set; } = null!;

    public string Role { get; set; } = null!;

    public string TokenHash { get; set; } = null!;

    public Guid? InvitedById { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? AcceptedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual User? InvitedBy { get; set; }

    public virtual Workspace Workspace { get; set; } = null!;
}
