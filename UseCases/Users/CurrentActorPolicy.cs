using Kawa.Abstractions;

namespace VrcWebMap.Backend.UseCases.Users;

/// <summary>
/// 認証済みユーザーに共通する書き込み条件を検証します。
/// </summary>
public static class CurrentActorPolicy
{
    public static KawaError? RequireWriter(
        ICurrentActorAccessor accessor,
        out CurrentActor? actor)
    {
        actor = accessor.GetCurrent();
        if (actor is null)
        {
            return new KawaError(KawaErrorKind.Forbidden, "Discordログインが必要です。");
        }

        if (!actor.HasVRChatDisplayName)
        {
            return new KawaError(
                KawaErrorKind.Forbidden,
                "書き込みにはVRChat表示名の登録が必要です。");
        }

        return null;
    }
}
