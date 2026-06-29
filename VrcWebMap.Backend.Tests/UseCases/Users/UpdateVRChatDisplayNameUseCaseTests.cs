using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.Users;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.Tests.TestDoubles;
using VrcWebMap.Backend.UseCases.Users;

namespace VrcWebMap.Backend.Tests.UseCases.Users;

public sealed class UpdateVRChatDisplayNameUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ValidName_StoresTrimmedDisplayAndNormalizedKey()
    {
        var repository = new FakeDiscordUserRepository(CreateUser("actor", "discord-user"));
        var useCase = new UpdateVRChatDisplayNameUseCase(
            repository,
            new FakeCurrentActorAccessor(new CurrentActor("actor", IsAdmin: false, HasVRChatDisplayName: false)));

        var result = await useCase.ExecuteAsync(new UpdateVRChatDisplayName.Request(" Ａlice "));

        Assert.True(result.IsSuccess);
        Assert.Equal("Ａlice", result.Value!.User.VRChatDisplayName);
        Assert.Equal("ALICE", result.Value.User.NormalizedVRChatDisplayName);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("abcdefghijklmnop")]
    public async Task ExecuteAsync_NameOutsideVrChatLength_ReturnsValidation(string displayName)
    {
        var repository = new FakeDiscordUserRepository(CreateUser("actor", "discord-user"));
        var useCase = new UpdateVRChatDisplayNameUseCase(
            repository,
            new FakeCurrentActorAccessor(new CurrentActor("actor", IsAdmin: false, HasVRChatDisplayName: false)));

        var result = await useCase.ExecuteAsync(new UpdateVRChatDisplayName.Request(displayName));

        Assert.True(result.IsFailure);
        Assert.Equal(KawaErrorKind.Validation, result.Error!.Kind);
    }

    [Fact]
    public async Task ExecuteAsync_NormalizedNameOwnedByAnotherUser_ReturnsConflict()
    {
        var repository = new FakeDiscordUserRepository(
            CreateUser("actor", "discord-user"),
            CreateUser("other", "other-user") with
            {
                VRChatDisplayName = "Alice",
                NormalizedVRChatDisplayName = "ALICE"
            });
        var useCase = new UpdateVRChatDisplayNameUseCase(
            repository,
            new FakeCurrentActorAccessor(new CurrentActor("actor", IsAdmin: false, HasVRChatDisplayName: false)));

        var result = await useCase.ExecuteAsync(new UpdateVRChatDisplayName.Request("alice"));

        Assert.True(result.IsFailure);
        Assert.Equal(KawaErrorKind.Conflict, result.Error!.Kind);
    }

    [Fact]
    public async Task ExecuteAsync_Unauthenticated_ReturnsForbidden()
    {
        var repository = new FakeDiscordUserRepository(CreateUser("actor", "discord-user"));
        var useCase = new UpdateVRChatDisplayNameUseCase(
            repository,
            new FakeCurrentActorAccessor(null));

        var result = await useCase.ExecuteAsync(new UpdateVRChatDisplayName.Request("Alice"));

        Assert.True(result.IsFailure);
        Assert.Equal(KawaErrorKind.Forbidden, result.Error!.Kind);
    }

    internal static DiscordUser CreateUser(string id, string username, bool isAdmin = false)
    {
        var now = DateTimeOffset.UtcNow;
        return new DiscordUser(
            id,
            username,
            GlobalName: null,
            AvatarHash: null,
            RequiredGuildId: "guild",
            IsGuildMember: true,
            IsAdmin: isAdmin,
            now,
            now);
    }
}
