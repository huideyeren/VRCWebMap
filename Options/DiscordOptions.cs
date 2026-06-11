namespace VrcWebMap.Backend.Options;

/// <summary>
/// Discord OAuth と参加必須サーバーの設定です。
/// </summary>
public sealed class DiscordOptions
{
    public string ClientId { get; init; } = string.Empty;

    public string ClientSecret { get; init; } = string.Empty;

    public string RedirectUri { get; init; } = string.Empty;

    public string RequiredGuildId { get; init; } = string.Empty;

    public string BotToken { get; init; } = string.Empty;

    public string AdminRoleName { get; init; } = "マップ管理者";
}
