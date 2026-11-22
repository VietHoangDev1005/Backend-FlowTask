using System;
using System.Collections.Generic;

namespace TaskFlow.Domain.Entities;

public partial class Message
{
    public Guid Id { get; set; }

    public Guid ChatId { get; set; }

    public Guid? SenderId { get; set; }

    public string? SenderNameSnapshot { get; set; }

    public string Content { get; set; } = null!;

    public DateTime? EditedAt { get; set; }

    public long RowVersion { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Chat Chat { get; set; } = null!;

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual User? Sender { get; set; }
}
