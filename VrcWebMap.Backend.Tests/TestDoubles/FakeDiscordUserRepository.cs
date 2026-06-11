using System.Diagnostics.CodeAnalysis;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.UseCases.Users;

namespace VrcWebMap.Backend.Tests.TestDoubles;

internal sealed class FakeDiscordUserRepository(params DiscordUser[] initialUsers) : IDiscordUserRepository
{
    private readonly Dictionary<string, DiscordUser> users = initialUsers.ToDictionary(user => user.DiscordUserId);

    public List<DiscordUser> SavedUsers { get; } = [];

    public bool TryGetByDiscordUserId(string discordUserId, [NotNullWhen(true)] out DiscordUser? user) =>
        users.TryGetValue(discordUserId, out user);

    public void Upsert(DiscordUser user)
    {
        users[user.DiscordUserId] = user;
        SavedUsers.Add(user);
    }
}
