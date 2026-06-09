namespace VrcWebMap.Backend.Contracts.VRChatWorlds;

public static class DeleteVRChatWorld
{
    public sealed record Request(Guid Id, string ActorUserId, bool ActorIsAdmin);

    public sealed record Response(Guid Id);
}
