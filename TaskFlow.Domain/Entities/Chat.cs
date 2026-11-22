using System;
using System.Collections.Generic;

namespace TaskFlow.Domain.Entities;

public partial class Chat
{
    public Guid Id { get; set; }

    public string Type { get; set; } = null!;

    public Guid? WorkspaceId { get; set; }

    public Guid? UserAId { get; set; }

    public Guid? UserBId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<ChatRead> ChatReads { get; set; } = new List<ChatRead>();

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    public virtual User? UserA { get; set; }

    public virtual User? UserB { get; set; }

    public virtual Workspace? Workspace { get; set; }
}
