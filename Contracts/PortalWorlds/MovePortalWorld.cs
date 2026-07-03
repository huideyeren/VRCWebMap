using VrcWebMap.Backend.Contracts.VRChatWorlds;

namespace VrcWebMap.Backend.Contracts.PortalWorlds;

/// <summary>
/// 地図外ワールドを別のカテゴリへ移動する契約です。
/// </summary>
public static class MovePortalWorld
{
    public sealed record Request(Guid Id, Guid DestinationPortalCategoryId);

    public sealed record Response(VRChatWorldData World);
}
