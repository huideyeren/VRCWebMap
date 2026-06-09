using VrcWebMap.Backend.Models;

namespace VrcWebMap.Backend.Contracts.Comments;

public static class UpdateComment
{
    public sealed record Request(Guid Id, string ActorUserId, bool ActorIsAdmin, string Comments);

    public sealed record Response(Comment Comment);
}
