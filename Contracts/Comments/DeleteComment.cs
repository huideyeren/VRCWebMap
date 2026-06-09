namespace VrcWebMap.Backend.Contracts.Comments;

public static class DeleteComment
{
    public sealed record Request(Guid Id, string ActorUserId, bool ActorIsAdmin);

    public sealed record Response(Guid Id);
}
