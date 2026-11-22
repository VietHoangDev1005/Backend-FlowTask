using System;
using System.Collections.Generic;

namespace TaskFlow.Domain.Entities;

public partial class User
{
    public Guid Id { get; set; }

    public string Email { get; set; } = null!;

    public DateTime? EmailConfirmedAt { get; set; }

    public bool? IsVerified { get; set; }

    public string DisplayName { get; set; } = null!;

    public string? FullName { get; set; }

    public string? Phone { get; set; }

    public DateOnly? Birthday { get; set; }

    public string? AvatarUrl { get; set; }

    public string? AvatarPublicId { get; set; }

    public string? TimeZoneId { get; set; }

    public string? PasswordHash { get; set; }

    public int AccessFailedCount { get; set; }

    public DateTime? LockoutEnd { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public string? PushToken { get; set; }

    public string? PushPlatform { get; set; }

    public string NotificationSettings { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? Provider { get; set; }

    public string? ProviderUserId { get; set; }

    public virtual ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();

    public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();

    public virtual ICollection<BoardStar> BoardStars { get; set; } = new List<BoardStar>();

    public virtual ICollection<Board> Boards { get; set; } = new List<Board>();

    public virtual ICollection<CardAssignee> CardAssigneeAssignedBies { get; set; } = new List<CardAssignee>();

    public virtual ICollection<CardAssignee> CardAssigneeUsers { get; set; } = new List<CardAssignee>();

    public virtual ICollection<CardWatcher> CardWatchers { get; set; } = new List<CardWatcher>();

    public virtual ICollection<Card> Cards { get; set; } = new List<Card>();

    public virtual ICollection<ChatRead> ChatReads { get; set; } = new List<ChatRead>();

    public virtual ICollection<Chat> ChatUserAs { get; set; } = new List<Chat>();

    public virtual ICollection<Chat> ChatUserBs { get; set; } = new List<Chat>();

    public virtual ICollection<ChecklistItem> ChecklistItems { get; set; } = new List<ChecklistItem>();

    public virtual ICollection<CommentReaction> CommentReactions { get; set; } = new List<CommentReaction>();

    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public virtual ICollection<Invitation> Invitations { get; set; } = new List<Invitation>();

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    public virtual ICollection<Notification> NotificationActors { get; set; } = new List<Notification>();

    public virtual ICollection<Notification> NotificationUsers { get; set; } = new List<Notification>();

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public virtual Workspace? Workspace { get; set; }

    public virtual ICollection<WorkspaceMember> WorkspaceMembers { get; set; } = new List<WorkspaceMember>();
}
