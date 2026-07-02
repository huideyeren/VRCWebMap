using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.VRChatWorlds;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.Tests.TestDoubles;
using VrcWebMap.Backend.UseCases.Users;
using VrcWebMap.Backend.UseCases.VRChatWorlds;

namespace VrcWebMap.Backend.Tests.UseCases.VRChatWorlds;

public sealed class CreateVRChatWorldUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ExistingSpot_CreatesWorld()
    {
        var spot = new Spot(Guid.NewGuid(), "owner-user", "スポット", 35, 139, AreaCodes.Japan.Tokyo, "説明");
        var repository = new FakeSpotRepository(spot);
        var useCase = CreateUseCase(repository);
        var request = new CreateVRChatWorld.Request(
            spot.Id,
            " wrld_00000000-0000-0000-0000-000000000000 ",
            " テストワールド ",
            16,
            32,
            " 説明 ",
            PC: true,
            Android: false,
            IOS: false);

        var result = await useCase.ExecuteAsync(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("VRChat User", result.Value.World.RegisteredByDisplayName);
        Assert.True(result.Value.World.CanEdit);
        Assert.Equal("wrld_00000000-0000-0000-0000-000000000000", result.Value.World.VRChatWorldId);
        Assert.False(result.Value.World.IsPrivate);
        var saved = Assert.Single(repository.SavedWorlds);
        Assert.Equal(spot.Id, saved.SpotId);
        Assert.Equal("discord-user-id", saved.RegisteredByUserId);
    }

    [Fact]
    public async Task ExecuteAsync_EmptySpotId_ReturnsValidation()
    {
        var repository = new FakeSpotRepository();
        var useCase = CreateUseCase(repository);
        var request = new CreateVRChatWorld.Request(
            Guid.Empty,
            "wrld_00000000-0000-0000-0000-000000000000",
            "テストワールド",
            16,
            32,
            "説明",
            PC: true,
            Android: false,
            IOS: false);

        var result = await useCase.ExecuteAsync(request);

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(KawaErrorKind.Validation, result.Error.Kind);
        Assert.Empty(repository.SavedWorlds);
    }

    [Fact]
    public async Task ExecuteAsync_MissingSpot_ReturnsNotFound()
    {
        var repository = new FakeSpotRepository();
        var useCase = CreateUseCase(repository);
        var request = new CreateVRChatWorld.Request(
            Guid.NewGuid(),
            "wrld_00000000-0000-0000-0000-000000000000",
            "テストワールド",
            16,
            32,
            "説明",
            PC: true,
            Android: false,
            IOS: false);

        var result = await useCase.ExecuteAsync(request);

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(KawaErrorKind.NotFound, result.Error.Kind);
        Assert.Empty(repository.SavedWorlds);
    }

    private static CreateVRChatWorldUseCase CreateUseCase(FakeSpotRepository repository) =>
        new(
            repository,
            FakeDiscordUserRepository.WithVRChatDisplayName("discord-user-id"),
            new FakeCurrentActorAccessor(new CurrentActor(
                "discord-user-id",
                IsAdmin: false,
                HasVRChatDisplayName: true)));
}
