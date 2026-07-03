using VrcWebMap.Backend.Options;

namespace VrcWebMap.Backend.UseCases.Users;

/// <summary>
/// 初期管理者の判定規則を一か所に保持します。
/// </summary>
public static class AdministratorPolicy
{
    public static bool IsInitialAdministrator(DiscordOptions options, string discordUserId) =>
        options.InitialAdminUserIds.Any(
            id => string.Equals(id?.Trim(), discordUserId, StringComparison.Ordinal));
}
