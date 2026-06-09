using VrcWebMap.Backend.Models;

namespace VrcWebMap.Backend.Contracts.VRChatWorlds;

public static class UpdateVRChatWorld
{
    public sealed record Request(
        Guid Id,
        string ActorUserId,
        bool ActorIsAdmin,
        string VRChatWorldId,
        string Name,
        int RecommendedCapacity,
        int Capacity,
        string Description,
        bool PC,
        bool Android,
        bool IOS,
        bool IsPrivate = false);

    public sealed record Response(VRChatWorld World);
}
