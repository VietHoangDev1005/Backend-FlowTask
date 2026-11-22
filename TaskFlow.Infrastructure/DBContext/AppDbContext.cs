using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Infrastructure.DBContext;

public partial class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ActivityLog> ActivityLogs { get; set; }

    public virtual DbSet<Attachment> Attachments { get; set; }

    public virtual DbSet<Board> Boards { get; set; }

    public virtual DbSet<BoardColumn> BoardColumns { get; set; }

    public virtual DbSet<BoardStar> BoardStars { get; set; }

    public virtual DbSet<Card> Cards { get; set; }

    public virtual DbSet<CardAssignee> CardAssignees { get; set; }

    public virtual DbSet<CardWatcher> CardWatchers { get; set; }

    public virtual DbSet<Chat> Chats { get; set; }

    public virtual DbSet<ChatRead> ChatReads { get; set; }

    public virtual DbSet<Checklist> Checklists { get; set; }

    public virtual DbSet<ChecklistItem> ChecklistItems { get; set; }

    public virtual DbSet<Comment> Comments { get; set; }

    public virtual DbSet<CommentReaction> CommentReactions { get; set; }

    public virtual DbSet<Invitation> Invitations { get; set; }

    public virtual DbSet<Label> Labels { get; set; }

    public virtual DbSet<Message> Messages { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<VCard> VCards { get; set; }

    public virtual DbSet<VCardChecklistProgress> VCardChecklistProgresses { get; set; }

    public virtual DbSet<Workspace> Workspaces { get; set; }

    public virtual DbSet<WorkspaceMember> WorkspaceMembers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasPostgresExtension("pg_trgm")
            .HasPostgresExtension("unaccent");

        modelBuilder.Entity<ActivityLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("activity_logs_pkey");

            entity.ToTable("activity_logs");

            entity.HasIndex(e => new { e.BoardId, e.OccurredAt }, "ix_activity_board")
                .IsDescending(false, true)
                .HasFilter("(board_id IS NOT NULL)");

            entity.HasIndex(e => new { e.EntityType, e.EntityId, e.OccurredAt }, "ix_activity_entity").IsDescending(false, false, true);

            entity.HasIndex(e => new { e.WorkspaceId, e.OccurredAt }, "ix_activity_workspace").IsDescending(false, true);

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Action).HasColumnName("action");
            entity.Property(e => e.ActorId).HasColumnName("actor_id");
            entity.Property(e => e.BoardId).HasColumnName("board_id");
            entity.Property(e => e.EntityId).HasColumnName("entity_id");
            entity.Property(e => e.EntityType).HasColumnName("entity_type");
            entity.Property(e => e.Metadata)
                .HasDefaultValueSql("'{}'::jsonb")
                .HasColumnType("jsonb")
                .HasColumnName("metadata");
            entity.Property(e => e.OccurredAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("occurred_at");
            entity.Property(e => e.WorkspaceId).HasColumnName("workspace_id");

            entity.HasOne(d => d.Actor).WithMany(p => p.ActivityLogs)
                .HasForeignKey(d => d.ActorId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("activity_logs_actor_id_fkey");

            entity.HasOne(d => d.Workspace).WithMany(p => p.ActivityLogs)
                .HasForeignKey(d => d.WorkspaceId)
                .HasConstraintName("activity_logs_workspace_id_fkey");
        });

        modelBuilder.Entity<Attachment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("attachments_pkey");

            entity.ToTable("attachments");

            entity.HasIndex(e => new { e.CardId, e.CreatedAt }, "ix_attachments_card");

            entity.HasIndex(e => new { e.Id, e.CardId }, "ux_attachments_id_card").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CardId).HasColumnName("card_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.MimeType).HasColumnName("mime_type");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.PublicId).HasColumnName("public_id");
            entity.Property(e => e.SizeBytes).HasColumnName("size_bytes");
            entity.Property(e => e.Type).HasColumnName("type");
            entity.Property(e => e.UploadedById).HasColumnName("uploaded_by_id");
            entity.Property(e => e.Url).HasColumnName("url");

            entity.HasOne(d => d.Card).WithMany(p => p.Attachments)
                .HasForeignKey(d => d.CardId)
                .HasConstraintName("attachments_card_id_fkey");

            entity.HasOne(d => d.UploadedBy).WithMany(p => p.Attachments)
                .HasForeignKey(d => d.UploadedById)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("attachments_uploaded_by_id_fkey");
        });

        modelBuilder.Entity<Board>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("boards_pkey");

            entity.ToTable("boards");

            entity.HasIndex(e => e.WorkspaceId, "ix_boards_workspace").HasFilter("(NOT is_deleted)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.BackgroundColor).HasColumnName("background_color");
            entity.Property(e => e.BackgroundImageUrl).HasColumnName("background_image_url");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedById).HasColumnName("created_by_id");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsArchived).HasColumnName("is_archived");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.WorkspaceId).HasColumnName("workspace_id");

            entity.HasOne(d => d.CreatedBy).WithMany(p => p.Boards)
                .HasForeignKey(d => d.CreatedById)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("boards_created_by_id_fkey");

            entity.HasOne(d => d.Workspace).WithMany(p => p.Boards)
                .HasForeignKey(d => d.WorkspaceId)
                .HasConstraintName("boards_workspace_id_fkey");
        });

        modelBuilder.Entity<BoardColumn>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("board_columns_pkey");

            entity.ToTable("board_columns");

            entity.HasIndex(e => new { e.BoardId, e.Position }, "ix_board_columns_board_pos")
                .HasFilter("((NOT is_deleted) AND (NOT is_archived))")
                .UseCollation(new[] { null, "C" });

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ArchivedAt).HasColumnName("archived_at");
            entity.Property(e => e.BoardId).HasColumnName("board_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.IsArchived).HasColumnName("is_archived");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.IsDone).HasColumnName("is_done");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Position)
                .UseCollation("C")
                .HasColumnName("position");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Board).WithMany(p => p.BoardColumns)
                .HasForeignKey(d => d.BoardId)
                .HasConstraintName("board_columns_board_id_fkey");
        });

        modelBuilder.Entity<BoardStar>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.BoardId }).HasName("board_stars_pkey");

            entity.ToTable("board_stars");

            entity.HasIndex(e => e.BoardId, "ix_board_stars_board");

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.BoardId).HasColumnName("board_id");
            entity.Property(e => e.StarredAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("starred_at");

            entity.HasOne(d => d.Board).WithMany(p => p.BoardStars)
                .HasForeignKey(d => d.BoardId)
                .HasConstraintName("board_stars_board_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.BoardStars)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("board_stars_user_id_fkey");
        });

        modelBuilder.Entity<Card>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("cards_pkey");

            entity.ToTable("cards");

            entity.HasIndex(e => new { e.ColumnId, e.Position }, "ix_cards_column_position")
                .HasFilter("((NOT is_deleted) AND (NOT is_archived))")
                .UseCollation(new[] { null, "C" });

            entity.HasIndex(e => e.DueAt, "ix_cards_due_at").HasFilter("((NOT is_deleted) AND (NOT is_archived) AND (due_at IS NOT NULL))");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ArchivedAt).HasColumnName("archived_at");
            entity.Property(e => e.ColumnId).HasColumnName("column_id");
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
            entity.Property(e => e.CoverAttachmentId).HasColumnName("cover_attachment_id");
            entity.Property(e => e.CoverColor).HasColumnName("cover_color");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedById).HasColumnName("created_by_id");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.DueAt).HasColumnName("due_at");
            entity.Property(e => e.IsArchived).HasColumnName("is_archived");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.OwnerNote).HasColumnName("owner_note");
            entity.Property(e => e.Position)
                .UseCollation("C")
                .HasColumnName("position");
            entity.Property(e => e.Priority)
                .HasDefaultValueSql("'medium'::text")
                .HasColumnName("priority");
            entity.Property(e => e.RowVersion)
                .HasDefaultValue(1L)
                .HasColumnName("row_version");
            entity.Property(e => e.StartAt).HasColumnName("start_at");
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Column).WithMany(p => p.Cards)
                .HasForeignKey(d => d.ColumnId)
                .HasConstraintName("cards_column_id_fkey");

            entity.HasOne(d => d.CreatedBy).WithMany(p => p.Cards)
                .HasForeignKey(d => d.CreatedById)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("cards_created_by_id_fkey");

            entity.HasOne(d => d.Attachment).WithMany(p => p.Cards)
                .HasPrincipalKey(p => new { p.Id, p.CardId })
                .HasForeignKey(d => new { d.CoverAttachmentId, d.Id })
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_cards_cover_attachment");

            entity.HasMany(d => d.Labels).WithMany(p => p.Cards)
                .UsingEntity<Dictionary<string, object>>(
                    "CardLabel",
                    r => r.HasOne<Label>().WithMany()
                        .HasForeignKey("LabelId")
                        .HasConstraintName("card_labels_label_id_fkey"),
                    l => l.HasOne<Card>().WithMany()
                        .HasForeignKey("CardId")
                        .HasConstraintName("card_labels_card_id_fkey"),
                    j =>
                    {
                        j.HasKey("CardId", "LabelId").HasName("card_labels_pkey");
                        j.ToTable("card_labels");
                        j.HasIndex(new[] { "LabelId" }, "ix_card_labels_label");
                        j.IndexerProperty<Guid>("CardId").HasColumnName("card_id");
                        j.IndexerProperty<Guid>("LabelId").HasColumnName("label_id");
                    });
        });

        modelBuilder.Entity<CardAssignee>(entity =>
        {
            entity.HasKey(e => new { e.CardId, e.UserId }).HasName("card_assignees_pkey");

            entity.ToTable("card_assignees");

            entity.HasIndex(e => e.UserId, "ix_card_assignees_user");

            entity.Property(e => e.CardId).HasColumnName("card_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.AssignedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("assigned_at");
            entity.Property(e => e.AssignedById).HasColumnName("assigned_by_id");

            entity.HasOne(d => d.AssignedBy).WithMany(p => p.CardAssigneeAssignedBies)
                .HasForeignKey(d => d.AssignedById)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("card_assignees_assigned_by_id_fkey");

            entity.HasOne(d => d.Card).WithMany(p => p.CardAssignees)
                .HasForeignKey(d => d.CardId)
                .HasConstraintName("card_assignees_card_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.CardAssigneeUsers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("card_assignees_user_id_fkey");
        });

        modelBuilder.Entity<CardWatcher>(entity =>
        {
            entity.HasKey(e => new { e.CardId, e.UserId }).HasName("card_watchers_pkey");

            entity.ToTable("card_watchers");

            entity.HasIndex(e => e.UserId, "ix_card_watchers_user");

            entity.Property(e => e.CardId).HasColumnName("card_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");

            entity.HasOne(d => d.Card).WithMany(p => p.CardWatchers)
                .HasForeignKey(d => d.CardId)
                .HasConstraintName("card_watchers_card_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.CardWatchers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("card_watchers_user_id_fkey");
        });

        modelBuilder.Entity<Chat>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("chats_pkey");

            entity.ToTable("chats");

            entity.HasIndex(e => new { e.UserAId, e.UserBId }, "ux_chats_direct")
                .IsUnique()
                .HasFilter("(type = 'direct'::text)");

            entity.HasIndex(e => e.WorkspaceId, "ux_chats_workspace")
                .IsUnique()
                .HasFilter("(type = 'workspace'::text)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Type).HasColumnName("type");
            entity.Property(e => e.UserAId).HasColumnName("user_a_id");
            entity.Property(e => e.UserBId).HasColumnName("user_b_id");
            entity.Property(e => e.WorkspaceId).HasColumnName("workspace_id");

            entity.HasOne(d => d.UserA).WithMany(p => p.ChatUserAs)
                .HasForeignKey(d => d.UserAId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("chats_user_a_id_fkey");

            entity.HasOne(d => d.UserB).WithMany(p => p.ChatUserBs)
                .HasForeignKey(d => d.UserBId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("chats_user_b_id_fkey");

            entity.HasOne(d => d.Workspace).WithOne(p => p.Chat)
                .HasForeignKey<Chat>(d => d.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("chats_workspace_id_fkey");
        });

        modelBuilder.Entity<ChatRead>(entity =>
        {
            entity.HasKey(e => new { e.ChatId, e.UserId }).HasName("chat_reads_pkey");

            entity.ToTable("chat_reads");

            entity.Property(e => e.ChatId).HasColumnName("chat_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.LastReadAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("last_read_at");

            entity.HasOne(d => d.Chat).WithMany(p => p.ChatReads)
                .HasForeignKey(d => d.ChatId)
                .HasConstraintName("chat_reads_chat_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.ChatReads)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("chat_reads_user_id_fkey");
        });

        modelBuilder.Entity<Checklist>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("checklists_pkey");

            entity.ToTable("checklists");

            entity.HasIndex(e => new { e.CardId, e.Position }, "ix_checklists_card").UseCollation(new[] { null, "C" });

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CardId).HasColumnName("card_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Position)
                .UseCollation("C")
                .HasColumnName("position");
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Card).WithMany(p => p.Checklists)
                .HasForeignKey(d => d.CardId)
                .HasConstraintName("checklists_card_id_fkey");
        });

        modelBuilder.Entity<ChecklistItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("checklist_items_pkey");

            entity.ToTable("checklist_items");

            entity.HasIndex(e => new { e.ChecklistId, e.Position }, "ix_checklist_items_list").UseCollation(new[] { null, "C" });

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CheckedAt).HasColumnName("checked_at");
            entity.Property(e => e.CheckedById).HasColumnName("checked_by_id");
            entity.Property(e => e.ChecklistId).HasColumnName("checklist_id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.IsChecked).HasColumnName("is_checked");
            entity.Property(e => e.Position)
                .UseCollation("C")
                .HasColumnName("position");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.CheckedBy).WithMany(p => p.ChecklistItems)
                .HasForeignKey(d => d.CheckedById)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("checklist_items_checked_by_id_fkey");

            entity.HasOne(d => d.Checklist).WithMany(p => p.ChecklistItems)
                .HasForeignKey(d => d.ChecklistId)
                .HasConstraintName("checklist_items_checklist_id_fkey");
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("comments_pkey");

            entity.ToTable("comments");

            entity.HasIndex(e => new { e.CardId, e.CreatedAt }, "ix_comments_card").HasFilter("(NOT is_deleted)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AuthorId).HasColumnName("author_id");
            entity.Property(e => e.AuthorNameSnapshot).HasColumnName("author_name_snapshot");
            entity.Property(e => e.CardId).HasColumnName("card_id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.EditedAt).HasColumnName("edited_at");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.RowVersion)
                .HasDefaultValue(1L)
                .HasColumnName("row_version");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Author).WithMany(p => p.Comments)
                .HasForeignKey(d => d.AuthorId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("comments_author_id_fkey");

            entity.HasOne(d => d.Card).WithMany(p => p.Comments)
                .HasForeignKey(d => d.CardId)
                .HasConstraintName("comments_card_id_fkey");
        });

        modelBuilder.Entity<CommentReaction>(entity =>
        {
            entity.HasKey(e => new { e.CommentId, e.UserId, e.Emoji }).HasName("comment_reactions_pkey");

            entity.ToTable("comment_reactions");

            entity.HasIndex(e => e.UserId, "ix_comment_reactions_user");

            entity.Property(e => e.CommentId).HasColumnName("comment_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Emoji).HasColumnName("emoji");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");

            entity.HasOne(d => d.Comment).WithMany(p => p.CommentReactions)
                .HasForeignKey(d => d.CommentId)
                .HasConstraintName("comment_reactions_comment_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.CommentReactions)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("comment_reactions_user_id_fkey");
        });

        modelBuilder.Entity<Invitation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("invitations_pkey");

            entity.ToTable("invitations");

            entity.HasIndex(e => e.TokenHash, "ux_invitations_token").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AcceptedAt).HasColumnName("accepted_at");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.InvitedById).HasColumnName("invited_by_id");
            entity.Property(e => e.Role).HasColumnName("role");
            entity.Property(e => e.TokenHash).HasColumnName("token_hash");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.WorkspaceId).HasColumnName("workspace_id");

            entity.HasOne(d => d.InvitedBy).WithMany(p => p.Invitations)
                .HasForeignKey(d => d.InvitedById)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("invitations_invited_by_id_fkey");

            entity.HasOne(d => d.Workspace).WithMany(p => p.Invitations)
                .HasForeignKey(d => d.WorkspaceId)
                .HasConstraintName("invitations_workspace_id_fkey");
        });

        modelBuilder.Entity<Label>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("labels_pkey");

            entity.ToTable("labels");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Color).HasColumnName("color");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.WorkspaceId).HasColumnName("workspace_id");

            entity.HasOne(d => d.Workspace).WithMany(p => p.Labels)
                .HasForeignKey(d => d.WorkspaceId)
                .HasConstraintName("labels_workspace_id_fkey");
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("messages_pkey");

            entity.ToTable("messages");

            entity.HasIndex(e => new { e.ChatId, e.CreatedAt, e.Id }, "ix_messages_chat")
                .IsDescending(false, true, true)
                .HasFilter("(NOT is_deleted)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ChatId).HasColumnName("chat_id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.EditedAt).HasColumnName("edited_at");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.RowVersion)
                .HasDefaultValue(1L)
                .HasColumnName("row_version");
            entity.Property(e => e.SenderId).HasColumnName("sender_id");
            entity.Property(e => e.SenderNameSnapshot).HasColumnName("sender_name_snapshot");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Chat).WithMany(p => p.Messages)
                .HasForeignKey(d => d.ChatId)
                .HasConstraintName("messages_chat_id_fkey");

            entity.HasOne(d => d.Sender).WithMany(p => p.Messages)
                .HasForeignKey(d => d.SenderId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("messages_sender_id_fkey");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("notifications_pkey");

            entity.ToTable("notifications");

            entity.HasIndex(e => e.CreatedAt, "ix_notifications_created");

            entity.HasIndex(e => new { e.UserId, e.CreatedAt }, "ix_notifications_user_created").IsDescending(false, true);

            entity.HasIndex(e => new { e.UserId, e.ReadAt }, "ix_notifications_user_read");

            entity.HasIndex(e => new { e.UserId, e.CardId, e.DueAt }, "ux_notifications_due_soon")
                .IsUnique()
                .HasFilter("(type = 'due_soon'::text)");

            entity.HasIndex(e => new { e.UserId, e.CardId }, "ux_notifications_note_unread")
                .IsUnique()
                .HasFilter("((type = 'note_updated'::text) AND (read_at IS NULL))");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ActorId).HasColumnName("actor_id");
            entity.Property(e => e.CardId).HasColumnName("card_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DueAt).HasColumnName("due_at");
            entity.Property(e => e.MessageId).HasColumnName("message_id");
            entity.Property(e => e.ReadAt).HasColumnName("read_at");
            entity.Property(e => e.Type).HasColumnName("type");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Actor).WithMany(p => p.NotificationActors)
                .HasForeignKey(d => d.ActorId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("notifications_actor_id_fkey");

            entity.HasOne(d => d.Card).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.CardId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("notifications_card_id_fkey");

            entity.HasOne(d => d.Message).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.MessageId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("notifications_message_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.NotificationUsers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("notifications_user_id_fkey");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("refresh_tokens_pkey");

            entity.ToTable("refresh_tokens");

            entity.HasIndex(e => e.SessionId, "ix_refresh_tokens_session");

            entity.HasIndex(e => e.UserId, "ix_refresh_tokens_user");

            entity.HasIndex(e => e.TokenHash, "ux_refresh_tokens_hash").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Client).HasColumnName("client");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.ReplacedByTokenId).HasColumnName("replaced_by_token_id");
            entity.Property(e => e.RevokedAt).HasColumnName("revoked_at");
            entity.Property(e => e.SessionId).HasColumnName("session_id");
            entity.Property(e => e.TokenHash).HasColumnName("token_hash");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.ReplacedByToken).WithMany(p => p.InverseReplacedByToken)
                .HasForeignKey(d => d.ReplacedByTokenId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("refresh_tokens_replaced_by_token_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.RefreshTokens)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("refresh_tokens_user_id_fkey");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("users_pkey");

            entity.ToTable("users");

            entity.HasIndex(e => e.CreatedAt, "ix_users_unverified").HasFilter("(email_confirmed_at IS NULL)");

            entity.HasIndex(e => new { e.Provider, e.ProviderUserId }, "ux_users_provider")
                .IsUnique()
                .HasFilter("(provider IS NOT NULL)");

            entity.HasIndex(e => e.PushToken, "ux_users_push_token")
                .IsUnique()
                .HasFilter("(push_token IS NOT NULL)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AccessFailedCount).HasColumnName("access_failed_count");
            entity.Property(e => e.AvatarPublicId).HasColumnName("avatar_public_id");
            entity.Property(e => e.AvatarUrl).HasColumnName("avatar_url");
            entity.Property(e => e.Birthday).HasColumnName("birthday");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DisplayName).HasColumnName("display_name");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.EmailConfirmedAt).HasColumnName("email_confirmed_at");
            entity.Property(e => e.FullName).HasColumnName("full_name");
            entity.Property(e => e.IsVerified)
                .HasComputedColumnSql("(email_confirmed_at IS NOT NULL)", true)
                .HasColumnName("is_verified");
            entity.Property(e => e.LastLoginAt).HasColumnName("last_login_at");
            entity.Property(e => e.LockoutEnd).HasColumnName("lockout_end");
            entity.Property(e => e.NotificationSettings)
                .HasDefaultValueSql("'{\"due_soon\": true, \"mentioned\": true, \"note_updated\": true, \"card_assigned\": true, \"comment_added\": true}'::jsonb")
                .HasColumnType("jsonb")
                .HasColumnName("notification_settings");
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
            entity.Property(e => e.Phone).HasColumnName("phone");
            entity.Property(e => e.Provider).HasColumnName("provider");
            entity.Property(e => e.ProviderUserId).HasColumnName("provider_user_id");
            entity.Property(e => e.PushPlatform).HasColumnName("push_platform");
            entity.Property(e => e.PushToken).HasColumnName("push_token");
            entity.Property(e => e.TimeZoneId).HasColumnName("time_zone_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<VCard>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_cards");

            entity.Property(e => e.BoardId).HasColumnName("board_id");
            entity.Property(e => e.BoardName).HasColumnName("board_name");
            entity.Property(e => e.ColumnId).HasColumnName("column_id");
            entity.Property(e => e.ColumnIsDone).HasColumnName("column_is_done");
            entity.Property(e => e.ColumnName).HasColumnName("column_name");
            entity.Property(e => e.DueAt).HasColumnName("due_at");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.IsDueSoon).HasColumnName("is_due_soon");
            entity.Property(e => e.IsOverdue).HasColumnName("is_overdue");
            entity.Property(e => e.Position)
                .UseCollation("C")
                .HasColumnName("position");
            entity.Property(e => e.Priority).HasColumnName("priority");
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.WorkspaceId).HasColumnName("workspace_id");
        });

        modelBuilder.Entity<VCardChecklistProgress>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_card_checklist_progress");

            entity.Property(e => e.CardId).HasColumnName("card_id");
            entity.Property(e => e.CheckedItems).HasColumnName("checked_items");
            entity.Property(e => e.TotalItems).HasColumnName("total_items");
        });

        modelBuilder.Entity<Workspace>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("workspaces_pkey");

            entity.ToTable("workspaces");

            entity.HasIndex(e => e.CreatedById, "ux_workspaces_personal")
                .IsUnique()
                .HasFilter("is_personal");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ChatEnabled)
                .HasDefaultValue(true)
                .HasColumnName("chat_enabled");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedById).HasColumnName("created_by_id");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.IsPersonal).HasColumnName("is_personal");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.CreatedBy).WithOne(p => p.Workspace)
                .HasForeignKey<Workspace>(d => d.CreatedById)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("workspaces_created_by_id_fkey");
        });

        modelBuilder.Entity<WorkspaceMember>(entity =>
        {
            entity.HasKey(e => new { e.WorkspaceId, e.UserId }).HasName("workspace_members_pkey");

            entity.ToTable("workspace_members");

            entity.HasIndex(e => e.UserId, "ix_workspace_members_user");

            entity.Property(e => e.WorkspaceId).HasColumnName("workspace_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.JoinedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("joined_at");
            entity.Property(e => e.Role).HasColumnName("role");

            entity.HasOne(d => d.User).WithMany(p => p.WorkspaceMembers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("workspace_members_user_id_fkey");

            entity.HasOne(d => d.Workspace).WithMany(p => p.WorkspaceMembers)
                .HasForeignKey(d => d.WorkspaceId)
                .HasConstraintName("workspace_members_workspace_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
