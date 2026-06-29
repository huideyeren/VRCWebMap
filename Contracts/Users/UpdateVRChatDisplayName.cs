using VrcWebMap.Backend.Models;

namespace VrcWebMap.Backend.Contracts.Users;

/// <summary>
/// 現在ユーザーのVRChat表示名を登録または変更する契約です。
/// </summary>
public static class UpdateVRChatDisplayName
{
    /// <param name="VRChatDisplayName">VRChat内で表示される一意のDisplay Nameです。</param>
    public sealed record Request(string VRChatDisplayName);

    /// <param name="User">更新後のアプリケーションユーザーです。</param>
    public sealed record Response(DiscordUser User);
}
