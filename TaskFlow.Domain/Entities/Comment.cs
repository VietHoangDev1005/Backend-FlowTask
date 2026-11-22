using System;
using System.Collections.Generic;

namespace TaskFlow.Domain.Entities;

public partial class Comment
{
    public Guid Id { get; set; }

    public Guid CardId { get; set; }

    public Guid? AuthorId { get; set; }

    public string? AuthorNameSnapshot { get; set; }

    public string Content { get; set; } = null!;

    public DateTime? EditedAt { get; set; }

    public long RowVersion { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual User? Author { get; set; }

    public virtual Card Card { get; set; } = null!;

    public virtual ICollection<CommentReaction> CommentReactions { get; set; } = new List<CommentReaction>();
}
