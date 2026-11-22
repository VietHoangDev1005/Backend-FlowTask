using System;
using System.Collections.Generic;

namespace TaskFlow.Domain.Entities;

public partial class Label
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public string Name { get; set; } = null!;

    public string Color { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Workspace Workspace { get; set; } = null!;

    public virtual ICollection<Card> Cards { get; set; } = new List<Card>();
}
