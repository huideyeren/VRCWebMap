using VrcWebMap.Backend.Contracts.VRChatWorlds;

namespace VrcWebMap.Backend.Contracts.PortalWorlds;

/// <summary>
/// 地図外カテゴリへVRChatワールドを登録する契約です。
/// </summary>
public static class CreatePortalWorld
{
    public sealed record Request(
        Guid PortalCategoryId,
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
