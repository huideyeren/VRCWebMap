using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.UseCases.Users;

namespace VrcWebMap.Backend.Stores;

/// <summary>
/// 試作用のインメモリ Discord ユーザーリポジトリです。
/// </summary>
public sealed class InMemoryDiscordUserRepository : IDiscordUserRepository
{
    private readonly ConcurrentDictionary<string, DiscordUser> users = new(StringComparer.Ordinal);

    public DiscordUser[] List() =>
        users.Values
            .OrderBy(user => user.VRChatDisplayName is null)
            .ThenBy(user => user.NormalizedVRChatDisplayName, StringComparer.Ordinal)
            .ThenBy(user => user.Username, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    public bool TryGetByDiscordUserId(string discordUserId, [NotNullWhen(true)] out DiscordUser? user) =>
        users.TryGetValue(discordUserId, out user);

    public bool TryGetByNormalizedVRChatDisplayName(
        string normalizedVRChatDisplayName,
        [NotNullWhen(true)] out DiscordUser? user)
    {
        user = users.Values.FirstOrDefault(
            candidate => string.Equals(
                candidate.NormalizedVRChatDisplayName,
                normalizedVRChatDisplayName,
                StringComparison.Ordinal));
        return user is not null;
    }

    public void Upsert(DiscordUser user)
    {
        users[user.DiscordUserId] = user;
    }
}
