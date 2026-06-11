using VrcWebMap.Backend.Models;

namespace VrcWebMap.Backend.Contracts.Users;

/// <summary>
/// Discord OAuth callback adapter が確認済み Discord ユーザーを登録する契約です。
/// </summary>
public static class RegisterDiscordUser
{
    /// <summary>
    /// Discord API で確認済みのユーザー情報です。
    /// </summary>
    public sealed record Request(
        string DiscordUserId,
        string Username,
        string? GlobalName,
        string? AvatarHash,
        string RequiredGuildId,
        bool IsRequiredGuildMember,
        bool IsAdmin = false);

    /// <summary>
    /// 登録または更新された Discord ユーザーです。
    /// </summary>
    public sealed record Response(DiscordUser User);
}
