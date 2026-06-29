using VrcWebMap.Backend.Models;

namespace VrcWebMap.Backend.Contracts.Users;

/// <summary>
/// 管理者が他ユーザーの管理者状態を変更する契約です。
/// </summary>
public static class SetUserAdminStatus
{
    public sealed record Request(string DiscordUserId, bool IsAdmin);

    public sealed record Response(DiscordUser User);
}
