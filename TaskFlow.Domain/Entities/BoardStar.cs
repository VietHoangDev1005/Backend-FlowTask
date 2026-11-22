using System;
using System.Collections.Generic;

namespace TaskFlow.Domain.Entities;

public partial class BoardStar
{
    public Guid UserId { get; set; }

    public Guid BoardId { get; set; }

    public DateTime StarredAt { get; set; }

    public virtual Board Board { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
