using System;
using System.Collections.Generic;

namespace TaskFlow.Domain.Entities;

public partial class CardAssignee
{
    public Guid CardId { get; set; }

    public Guid UserId { get; set; }

    public Guid? AssignedById { get; set; }

    public DateTime AssignedAt { get; set; }

    public virtual User? AssignedBy { get; set; }

    public virtual Card Card { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
