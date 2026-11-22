using System;
using System.Collections.Generic;

namespace TaskFlow.Domain.Entities;

public partial class BoardColumn
{
    public Guid Id { get; set; }

    public Guid BoardId { get; set; }

    public string Name { get; set; } = null!;

    public string Position { get; set; } = null!;

    public bool IsDone { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public bool IsArchived { get; set; }

    public DateTime? ArchivedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Board Board { get; set; } = null!;

    public virtual ICollection<Card> Cards { get; set; } = new List<Card>();
}
