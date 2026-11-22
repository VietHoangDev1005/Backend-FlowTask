using System;
using System.Collections.Generic;

namespace TaskFlow.Domain.Entities;

public partial class ChecklistItem
{
    public Guid Id { get; set; }

    public Guid ChecklistId { get; set; }

    public string Content { get; set; } = null!;

    public bool IsChecked { get; set; }

    public DateTime? CheckedAt { get; set; }

    public Guid? CheckedById { get; set; }

    public string Position { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual User? CheckedBy { get; set; }

    public virtual Checklist Checklist { get; set; } = null!;
}
