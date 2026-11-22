using System;
using System.Collections.Generic;

namespace TaskFlow.Domain.Entities;

public partial class ChatRead
{
    public Guid ChatId { get; set; }

    public Guid UserId { get; set; }

    public DateTime LastReadAt { get; set; }

    public virtual Chat Chat { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
