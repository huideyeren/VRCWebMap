using VrcWebMap.Backend.Contracts.VRChatWorlds;

namespace VrcWebMap.Backend.Contracts.PortalWorlds;

/// <summary>
/// 地図外カテゴリのVRChatワールドを更新する契約です。
/// </summary>
public static class UpdatePortalWorld
{
    public sealed record Request(
        Guid Id,
        string VRChatWorldId,
        string Name,
        int RecommendedCapacity,
        int Capacity,
        string Description,
        bool PC,
        bool Android,
        bool IOS,
        bool IsPrivate = false);

    public sealed record Response(VRChatWorldData World);
}
