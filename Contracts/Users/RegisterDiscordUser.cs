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
    /// <param name="DiscordUserId">Discord API が返したユーザー ID です。</param>
    /// <param name="Username">Discord API が返したユーザー名です。</param>
    /// <param name="GlobalName">Discord API が返したグローバル表示名です。</param>
    /// <param name="AvatarHash">Discord API が返した avatar hash です。</param>
    /// <param name="RequiredGuildId">参加が必須の Discord guild ID です。</param>
    /// <param name="IsRequiredGuildMember">対象 guild への参加を server-side に確認できた場合は <c>true</c> です。</param>
    public sealed record Request(
        string DiscordUserId,
        string Username,
        string? GlobalName,
        string? AvatarHash,
        string RequiredGuildId,
        bool IsRequiredGuildMember);

    /// <summary>
    /// 登録または更新された Discord ユーザーです。
    /// </summary>
    /// <param name="User">登録または更新されたアプリケーションユーザーです。</param>
    public sealed record Response(DiscordUser User);
}
