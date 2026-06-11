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

    public bool TryGetByDiscordUserId(string discordUserId, [NotNullWhen(true)] out DiscordUser? user) =>
        users.TryGetValue(discordUserId, out user);

    public void Upsert(DiscordUser user)
    {
        users[user.DiscordUserId] = user;
    }
}
