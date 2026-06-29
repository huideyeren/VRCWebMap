using Kawa.Abstractions;
using Microsoft.Extensions.Options;
using VrcWebMap.Backend.Contracts.Users;
using VrcWebMap.Backend.Options;
using VrcWebMap.Backend.Tests.TestDoubles;
using VrcWebMap.Backend.UseCases.Users;

namespace VrcWebMap.Backend.Tests.UseCases.Users;

public sealed class ListUsersUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_Administrator_ReturnsUsersAndInitialAdministratorFlag()
    {
        var repository = new FakeDiscordUserRepository(
            UpdateVRChatDisplayNameUseCaseTests.CreateUser("admin", "admin", isAdmin: true),
            UpdateVRChatDisplayNameUseCaseTests.CreateUser("user", "user"));
        var useCase = new ListUsersUseCase(
            repository,
            new FakeCurrentActorAccessor(new CurrentActor("admin", IsAdmin: true, HasVRChatDisplayName: true)),
            Microsoft.Extensions.Options.Options.Create(new DiscordOptions
            {
                InitialAdminUserIds = ["admin"]
            }));

        var result = await useCase.ExecuteAsync(new ListUsers.Request());

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Users.Length);
        Assert.True(result.Value.Users.Single(user => user.DiscordUserId == "admin").IsInitialAdmin);
        Assert.False(result.Value.Users.Single(user => user.DiscordUserId == "user").IsInitialAdmin);
    }

    [Fact]
    public async Task ExecuteAsync_GeneralUser_ReturnsForbidden()
    {
        var repository = new FakeDiscordUserRepository(
            UpdateVRChatDisplayNameUseCaseTests.CreateUser("user", "user"));
        var useCase = new ListUsersUseCase(
            repository,
            new FakeCurrentActorAccessor(new CurrentActor("user", IsAdmin: false, HasVRChatDisplayName: true)),
            Microsoft.Extensions.Options.Options.Create(new DiscordOptions()));

        var result = await useCase.ExecuteAsync(new ListUsers.Request());

        Assert.True(result.IsFailure);
        Assert.Equal(KawaErrorKind.Forbidden, result.Error!.Kind);
    }
}
