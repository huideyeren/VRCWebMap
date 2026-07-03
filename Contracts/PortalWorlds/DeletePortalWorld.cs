namespace VrcWebMap.Backend.Contracts.PortalWorlds;

/// <summary>
/// 地図外カテゴリのVRChatワールドを削除する契約です。
/// </summary>
public static class DeletePortalWorld
{
    public sealed record Request(Guid Id);

    public sealed record Response(Guid Id);
}
