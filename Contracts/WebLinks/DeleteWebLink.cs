namespace VrcWebMap.Backend.Contracts.WebLinks;

public static class DeleteWebLink
{
    public sealed record Request(Guid Id, string ActorUserId, bool ActorIsAdmin);

    public sealed record Response(Guid Id);
}
