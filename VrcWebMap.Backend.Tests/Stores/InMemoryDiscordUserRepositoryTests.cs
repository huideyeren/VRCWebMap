using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.Stores;

namespace VrcWebMap.Backend.Tests.Stores;

public sealed class InMemoryDiscordUserRepositoryTests
{
    [Fact]
    public void List_ReturnsUsersOrderedByVrChatDisplayNameThenDiscordUsername()
    {
        var repository = new InMemoryDiscordUserRepository();
        repository.Upsert(CreateUser("3", "charlie", null, null));
        repository.Upsert(CreateUser("2", "bravo", "Zulu", "ZULU"));
        repository.Upsert(CreateUser("1", "alpha", "Alpha", "ALPHA"));

        var users = repository.List();

        Assert.Equal(["1", "2", "3"], users.Select(user => user.DiscordUserId));
    }

    [Fact]
    public void TryGetByNormalizedVRChatDisplayName_FindsRegisteredName()
    {
        var repository = new InMemoryDiscordUserRepository();
        repository.Upsert(CreateUser("1", "alpha", "るいざ", "るいざ"));

        var found = repository.TryGetByNormalizedVRChatDisplayName("るいざ", out var user);

        Assert.True(found);
        Assert.Equal("1", user!.DiscordUserId);
    }

    private static DiscordUser CreateUser(
        string discordUserId,
        string username,
        string? vrChatDisplayName,
        string? normalizedVRChatDisplayName)
    {
        var now = DateTimeOffset.UtcNow;
        return new DiscordUser(
            discordUserId,
            username,
            GlobalName: null,
            AvatarHash: null,
            RequiredGuildId: "guild",
            IsGuildMember: true,
            IsAdmin: false,
            now,
            now,
            vrChatDisplayName,
            normalizedVRChatDisplayName);
    }
}
