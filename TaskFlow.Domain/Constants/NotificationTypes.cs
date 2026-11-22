namespace TaskFlow.Domain.Constants;

// Giá trị khớp ràng buộc CHECK trong schema.sql: notifications.type
public static class NotificationTypes
{
    public const string CardAssigned = "card_assigned";
    public const string Mentioned = "mentioned";
    public const string DueSoon = "due_soon";
    public const string CommentAdded = "comment_added";
    public const string NoteUpdated = "note_updated";
    public const string CardUpdated = "card_updated";
}
