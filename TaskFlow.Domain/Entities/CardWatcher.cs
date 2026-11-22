using System;
using System.Collections.Generic;

namespace TaskFlow.Domain.Entities;

public partial class CardWatcher
{
    public Guid CardId { get; set; }

    public Guid UserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Card Card { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
