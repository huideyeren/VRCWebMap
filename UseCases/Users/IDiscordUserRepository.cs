using System.Diagnostics.CodeAnalysis;
using VrcWebMap.Backend.Models;

namespace VrcWebMap.Backend.UseCases.Users;

/// <summary>
/// Discord ユーザー永続化の境界です。
/// </summary>
public interface IDiscordUserRepository
{
    DiscordUser[] List();

    bool TryGetByDiscordUserId(string discordUserId, [NotNullWhen(true)] out DiscordUser? user);

    bool TryGetByNormalizedVRChatDisplayName(
        string normalizedVRChatDisplayName,
        [NotNullWhen(true)] out DiscordUser? user);

    void Upsert(DiscordUser user);
}
