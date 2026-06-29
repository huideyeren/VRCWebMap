using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.Users;
using VrcWebMap.Backend.Options;
using VrcWebMap.Backend.Tests.TestDoubles;
using VrcWebMap.Backend.UseCases.Users;

namespace VrcWebMap.Backend.Tests.UseCases.Users;

public sealed class SetUserAdminStatusUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_Administrator_GrantsAdministratorStatus()
    {
        var repository = CreateRepository();
        var useCase = CreateUseCase(repository);

        var result = await useCase.ExecuteAsync(new SetUserAdminStatus.Request("user", IsAdmin: true));

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.User.IsAdmin);
    }

    [Fact]
    public async Task ExecuteAsync_GeneralUser_ReturnsForbidden()
    {
        var repository = CreateRepository();
        var useCase = new SetUserAdminStatusUseCase(
            repository,
            new FakeCurrentActorAccessor(new CurrentActor("user", IsAdmin: false, HasVRChatDisplayName: true)),
            Microsoft.Extensions.Options.Options.Create(new DiscordOptions
            {
                InitialAdminUserIds = ["admin"]
            }));

        var result = await useCase.ExecuteAsync(new SetUserAdminStatus.Request("user", IsAdmin: true));

        Assert.True(result.IsFailure);
        Assert.Equal(KawaErrorKind.Forbidden, result.Error!.Kind);
    }

    [Fact]
    public async Task ExecuteAsync_InitialAdministratorDemotion_ReturnsConflict()
    {
        var repository = CreateRepository();
        var useCase = CreateUseCase(repository);

        var result = await useCase.ExecuteAsync(new SetUserAdminStatus.Request("admin", IsAdmin: false));

        Assert.True(result.IsFailure);
        Assert.Equal(KawaErrorKind.Conflict, result.Error!.Kind);
    }

    [Fact]
    public async Task ExecuteAsync_SelfDemotion_ReturnsConflict()
    {
        var repository = new FakeDiscordUserRepository(
            UpdateVRChatDisplayNameUseCaseTests.CreateUser("initial", "initial", isAdmin: true),
            UpdateVRChatDisplayNameUseCaseTests.CreateUser("admin", "admin", isAdmin: true));
        var useCase = new SetUserAdminStatusUseCase(
            repository,
            new FakeCurrentActorAccessor(new CurrentActor("admin", IsAdmin: true, HasVRChatDisplayName: true)),
            Microsoft.Extensions.Options.Options.Create(new DiscordOptions
            {
                InitialAdminUserIds = ["initial"]
            }));

        var result = await useCase.ExecuteAsync(new SetUserAdminStatus.Request("admin", IsAdmin: false));

        Assert.True(result.IsFailure);
        Assert.Equal(KawaErrorKind.Conflict, result.Error!.Kind);
    }

    private static FakeDiscordUserRepository CreateRepository() =>
        new(
            UpdateVRChatDisplayNameUseCaseTests.CreateUser("admin", "admin", isAdmin: true),
            UpdateVRChatDisplayNameUseCaseTests.CreateUser("user", "user"));

    private static SetUserAdminStatusUseCase CreateUseCase(FakeDiscordUserRepository repository) =>
        new(
            repository,
            new FakeCurrentActorAccessor(new CurrentActor("admin", IsAdmin: true, HasVRChatDisplayName: true)),
            Microsoft.Extensions.Options.Options.Create(new DiscordOptions
            {
                InitialAdminUserIds = ["admin"]
            }));
}
