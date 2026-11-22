using System;
using System.Collections.Generic;

namespace TaskFlow.Domain.Entities;

public partial class CommentReaction
{
    public Guid CommentId { get; set; }

    public Guid UserId { get; set; }

    public string Emoji { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Comment Comment { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
