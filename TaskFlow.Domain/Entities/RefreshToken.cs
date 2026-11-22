using System;
using System.Collections.Generic;

namespace TaskFlow.Domain.Entities;

public partial class RefreshToken
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid SessionId { get; set; }

    public string Client { get; set; } = null!;

    public string TokenHash { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public Guid? ReplacedByTokenId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<RefreshToken> InverseReplacedByToken { get; set; } = new List<RefreshToken>();

    public virtual RefreshToken? ReplacedByToken { get; set; }

    public virtual User User { get; set; } = null!;
}
