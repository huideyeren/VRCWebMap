namespace VrcWebMap.Backend.UseCases.Users;

/// <summary>
/// 認証済みtransportがUseCaseへ渡す現在の操作ユーザーです。
/// </summary>
/// <param name="DiscordUserId">不変のDiscordユーザーIDです。</param>
/// <param name="IsAdmin">DBの最新状態で管理者の場合は <c>true</c> です。</param>
/// <param name="HasVRChatDisplayName">VRChat表示名を登録済みの場合は <c>true</c> です。</param>
public sealed record CurrentActor(
    string DiscordUserId,
    bool IsAdmin,
    bool HasVRChatDisplayName);
