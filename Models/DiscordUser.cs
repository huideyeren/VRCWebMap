namespace VrcWebMap.Backend.Models;

/// <summary>
/// Discord 認証で登録されたアプリケーションユーザーです。
/// </summary>
public sealed record DiscordUser(
    string DiscordUserId,
    string Username,
    string? GlobalName,
    string? AvatarHash,
    string RequiredGuildId,
    bool IsGuildMember,
    bool IsAdmin,
    DateTimeOffset RegisteredAt,
    DateTimeOffset LastSeenAt,
    string? VRChatDisplayName = null,
    string? NormalizedVRChatDisplayName = null);
