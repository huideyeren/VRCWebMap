namespace VrcWebMap.Backend.Contracts.Users;

/// <summary>
/// 管理者がアプリケーションユーザーを一覧する契約です。
/// </summary>
public static class ListUsers
{
    public sealed record Request;

    public sealed record Item(
        string DiscordUserId,
        string Username,
        string? VRChatDisplayName,
        bool IsAdmin,
        bool IsInitialAdmin);

    public sealed record Response(Item[] Users);
}
