using System;
using System.Collections.Generic;

namespace TaskFlow.Domain.Entities;

public partial class Card
{
    public Guid Id { get; set; }

    public Guid ColumnId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string? OwnerNote { get; set; }

    public string Position { get; set; } = null!;

    public string Priority { get; set; } = null!;

    public DateTime? StartAt { get; set; }

    public DateTime? DueAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string? CoverColor { get; set; }

    public Guid? CoverAttachmentId { get; set; }

    public bool IsArchived { get; set; }

    public DateTime? ArchivedAt { get; set; }

    public Guid? CreatedById { get; set; }

    public long RowVersion { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Attachment? Attachment { get; set; }

    public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();

    public virtual ICollection<CardAssignee> CardAssignees { get; set; } = new List<CardAssignee>();

    public virtual ICollection<CardWatcher> CardWatchers { get; set; } = new List<CardWatcher>();

    public virtual ICollection<Checklist> Checklists { get; set; } = new List<Checklist>();

    public virtual BoardColumn Column { get; set; } = null!;

    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public virtual User? CreatedBy { get; set; }

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<Label> Labels { get; set; } = new List<Label>();
}
