using System;
using System.Collections.Generic;

namespace TaskFlow.Domain.Entities;

public partial class VCard
{
    public Guid? Id { get; set; }

    public string? Title { get; set; }

    public string? Priority { get; set; }

    public DateTime? DueAt { get; set; }

    public string? Position { get; set; }

    public Guid? ColumnId { get; set; }

    public string? ColumnName { get; set; }

    public bool? ColumnIsDone { get; set; }

    public Guid? BoardId { get; set; }

    public string? BoardName { get; set; }

    public Guid? WorkspaceId { get; set; }

    public bool? IsOverdue { get; set; }

    public bool? IsDueSoon { get; set; }
}
