using System;
using System.Collections.Generic;

namespace TaskFlow.Domain.Entities;

public partial class Checklist
{
    public Guid Id { get; set; }

    public Guid CardId { get; set; }

    public string Title { get; set; } = null!;

    public string Position { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Card Card { get; set; } = null!;

    public virtual ICollection<ChecklistItem> ChecklistItems { get; set; } = new List<ChecklistItem>();
}
