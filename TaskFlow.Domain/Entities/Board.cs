using System;
using System.Collections.Generic;

namespace TaskFlow.Domain.Entities;

public partial class Board
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public string Name { get; set; } = null!;

    public Guid? CreatedById { get; set; }

    public bool IsArchived { get; set; }

    public string? Description { get; set; }

    public string? BackgroundColor { get; set; }

    public string? BackgroundImageUrl { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<BoardColumn> BoardColumns { get; set; } = new List<BoardColumn>();

    public virtual ICollection<BoardStar> BoardStars { get; set; } = new List<BoardStar>();

    public virtual User? CreatedBy { get; set; }

    public virtual Workspace Workspace { get; set; } = null!;
}
