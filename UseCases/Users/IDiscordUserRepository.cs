using System.Diagnostics.CodeAnalysis;
using VrcWebMap.Backend.Models;

namespace VrcWebMap.Backend.UseCases.Users;

/// <summary>
/// Discord ユーザー永続化の境界です。
/// </summary>
public interface IDiscordUserRepository
{
    bool TryGetByDiscordUserId(string discordUserId, [NotNullWhen(true)] out DiscordUser? user);

    void Upsert(DiscordUser user);
}
