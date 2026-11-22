using System;
using System.Collections.Generic;

namespace TaskFlow.Domain.Entities;

public partial class Attachment
{
    public Guid Id { get; set; }

    public Guid CardId { get; set; }

    public string Type { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Url { get; set; } = null!;

    public string? MimeType { get; set; }

    public long? SizeBytes { get; set; }

    public string? PublicId { get; set; }

    public Guid? UploadedById { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Card Card { get; set; } = null!;

    public virtual ICollection<Card> Cards { get; set; } = new List<Card>();

    public virtual User? UploadedBy { get; set; }
}
